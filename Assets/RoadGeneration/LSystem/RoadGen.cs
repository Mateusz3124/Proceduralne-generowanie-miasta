using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Advertisements;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.Rendering.HableCurve;

public class RoadGen : MonoBehaviour
{
    public int segment_count_limit = 2000;
    public float scale = 2.7f;

    const float BRANCH_ANGLE_DEVIATION = 3.0f; // degrees
    const float STRAIGHT_ANGLE_DEVIATION = 15.0f; // degrees
    // find point with highest noise value in <-30, 30> degrees angle range
    const float HIGHWAY_ANGLE_SEARCH_RANGE = 15.0f; // degrees
    const int HIGHWAY_SEGMENT_LENGTH = 400;
    const float DEFAULT_BRANCH_PROBABILITY = 0.7f;
    const float HIGHWAY_BRANCH_PROBABILITY = 0.05f;
    // Branches are made based on perlin noise
    // Higher noise value, more roads are there
    const float NORMAL_BRANCH_POPULATION_THRESHOLD = 0.5f;
    const float HIGHWAY_BRANCH_POPULATION_THRESHOLD = 0.1f;
    // Snap action reach
    const int MAX_SNAP_DISTANCE = 50;
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    const int NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY = 5;
    const int DEFAULT_SEGMENT_LENGTH = 300;
    [HideInInspector]
    public river_Control river;
    // borders to lsystem
    // has default values but its better to set manually
    public static Vector2 minCorner {get; set;} = new Vector2(0f, 0f);
    public static Vector2 maxCorner {get; set;} = new Vector2(512f, 512f);

    private float randomPopOffsetX;
    private float randomPopOffsetY;

    void Start() 
    {

    }

    public void MakeSegmentOnScene(Segment segment) 
    {
        GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Vector2 direction = segment.end - segment.start;
        float length = direction.magnitude;

        Vector2 positionVec = segment.start + 0.5f * direction;
        segmentObject.transform.position = new Vector3(positionVec.x, 100f, positionVec.y);

        segmentObject.transform.localScale = new Vector3(SEGMENT_COLLIDER_WIDTH, 0.01f, length);

        Vector2 directionVector = direction.normalized;
        segmentObject.transform.forward = new Vector3(directionVector.x, 0f, directionVector.y);
        segmentObject.GetComponent<Renderer>().material.color = segment.metadata.highway ? Color.red : Color.blue;
    }

    private float RandomBranchAngle()
    {
        return UnityEngine.Random.Range(-BRANCH_ANGLE_DEVIATION, BRANCH_ANGLE_DEVIATION);
    }

    private float RandomStraightContinueAngle()
    {
        return UnityEngine.Random.Range(-STRAIGHT_ANGLE_DEVIATION, STRAIGHT_ANGLE_DEVIATION);
    }

    public List<Segment> GenerateSegments(Vector2 center) 
    {
        // init randomPopOffset and get maxNoise value
        Vector2 maxNoisePos = getMaxNoisePos();

        List<Segment> segments = new List<Segment>();
        PriorityQueue<Segment, int> priorityQueue = new PriorityQueue<Segment, int>();
        // Main segment and opposite direction segment
        Segment mainSegment = new Segment(
            maxNoisePos, new Vector2(maxNoisePos.x + HIGHWAY_SEGMENT_LENGTH, maxNoisePos.y),
            0, new SegmentMetadata { highway = true });
        Segment oppositeDirectionSegment = new Segment(
            maxNoisePos, new Vector2(maxNoisePos.x - HIGHWAY_SEGMENT_LENGTH, maxNoisePos.y),
            0, new SegmentMetadata { highway = true });
        MakeSegmentOnScene(mainSegment);
        MakeSegmentOnScene(oppositeDirectionSegment);
        oppositeDirectionSegment.linksB.Add(mainSegment);
        mainSegment.linksB.Add(oppositeDirectionSegment);
        priorityQueue.Push(mainSegment, mainSegment.t);
        priorityQueue.Push(oppositeDirectionSegment, oppositeDirectionSegment.t);

        // make roads going from one point to all directions
        int nmbOfCenterSegments = 10;
        float angleStep = 360 / nmbOfCenterSegments;
        float angle = 0f;
        for(int i = 0; i < nmbOfCenterSegments; i++) {
            Segment s = Segment.CreateUsingDirection(
                maxNoisePos, angle, HIGHWAY_SEGMENT_LENGTH, 0, new SegmentMetadata { highway = true }
            );
            s.previousSegmentToLink = mainSegment;

            priorityQueue.Push(s, s.t);
            angle += angleStep;
        }

        while (priorityQueue.length > 0 && segments.Count < segment_count_limit)
        {
            // Segment with smallest t (highest priority)
            Segment minSegment = priorityQueue.Pop();

            // Accept or not by LocalConstraints
            bool isAccepted = LocalConstraints(minSegment, segments);
            if (isAccepted)
            {
                minSegment.SetupBranchLinks();
                minSegment.MakeCollider();
                segments.Add(minSegment);

                // Generate new segments by GlobalGoalsGenerate
                foreach (Segment newSegment in GlobalGoalsGenerate(minSegment))
                {
                    newSegment.t = minSegment.t + 1 + newSegment.t;
                    priorityQueue.Push(newSegment, newSegment.t);
                }
            }
        }

        return segments;
    }

    private bool LocalConstraints(Segment segment, List<Segment> segments)
    {
        Actions action = null;
        int actionPriority = 0;
        float? previousIntersectionDistanceSquared = null;

        // Check collision around segment
        Vector3 pos3d = segment.CalculatePhysicsShapeTransform().position;
        List<Segment> matches = PhysicObjects.OverlapCircleSegments(new Vector2(pos3d.x, pos3d.z),
                                                             segment.length * 0.5f + MAX_SNAP_DISTANCE);

        foreach (Segment colliding in matches)
        {
            // Intersection check
            if (actionPriority <= 4)
            {
                var intersection = segment.IntersectionWith(colliding);
                if (intersection != null)
                {
                    var intersectionDistanceSquared = ((Vector2)(intersection - segment.start)).sqrMagnitude;
                    if (!previousIntersectionDistanceSquared.HasValue ||
                        intersectionDistanceSquared < previousIntersectionDistanceSquared.Value)
                    {
                        previousIntersectionDistanceSquared = intersectionDistanceSquared;
                        actionPriority = 4;
                        action = new LocalConstraintsIntersectionAction(colliding, (Vector2)intersection);
                    }
                }
            }

            // Dont make dead ends, try connect to intersection
            if (actionPriority <= 3)
            {
                float distanceSquared = (colliding.end - segment.end).sqrMagnitude;
                if (distanceSquared <= MAX_SNAP_DISTANCE * MAX_SNAP_DISTANCE)
                {
                    actionPriority = 3;
                    action = new LocalConstraintsSnapAction(colliding, colliding.end);
                }
            }

            // Try connect segment.end to nearest point on colliding segment and check angle
            if (actionPriority <= 2)
            {
                if (PhysicObjects.IsPointInSegmentRange(segment.end, colliding.start, colliding.end))
                {
                    var intersection = PhysicObjects.GetClosestPointOnSegment(segment.end, colliding.start, colliding.end);
                    float distanceSquared = (segment.end - intersection).sqrMagnitude;
                    if (distanceSquared < MAX_SNAP_DISTANCE * MAX_SNAP_DISTANCE)
                    {
                        actionPriority = 2;
                        action = new LocalConstraintsIntersectionRadiusAction(colliding, intersection);
                    }
                }
            }
        }

        if (action != null)
        {
            return action.MakeAction(segment, segments);
        }
        return true;
    }

    private List<Segment> GlobalGoalsGenerate(Segment previousSegment) {
        List<Segment> newBranches = new List<Segment>();

        if(previousSegment.metadata.severed)
            return newBranches;

        Segment continueStraight = SegmentContinue(previousSegment, previousSegment.direction);
        float straightPop = SamplePopulation(continueStraight.start, continueStraight.end);

        if (previousSegment.metadata.highway)
        {
            Segment highestNoiseHighwayContinue = FindHighwayContinuation(previousSegment);
            newBranches.Add(highestNoiseHighwayContinue);
            float roadPop = SamplePopulation(highestNoiseHighwayContinue.start, highestNoiseHighwayContinue.end);

            if (roadPop > HIGHWAY_BRANCH_POPULATION_THRESHOLD)
            {
                if (UnityEngine.Random.Range(0f, 1f) < HIGHWAY_BRANCH_PROBABILITY)
                {
                    Segment leftHighwayBranch = SegmentContinue(
                        previousSegment, previousSegment.direction - 90f + RandomBranchAngle()
                    );
                    newBranches.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.Range(0f, 1f) < HIGHWAY_BRANCH_PROBABILITY)
                {
                    Segment rightHighwayBranch = SegmentContinue(
                        previousSegment, previousSegment.direction + 90f + RandomBranchAngle()
                    );
                    newBranches.Add(rightHighwayBranch);
                }
            }

        }
        else if (straightPop > NORMAL_BRANCH_POPULATION_THRESHOLD)
        {
            newBranches.Add(continueStraight);
        }

        if (straightPop > NORMAL_BRANCH_POPULATION_THRESHOLD)
        {
            if (UnityEngine.Random.Range(0f, 1f) < DEFAULT_BRANCH_PROBABILITY)
            {
                Segment leftBranch = SegmentBranch(
                    previousSegment, previousSegment.direction - 90f + RandomBranchAngle()
                );
                newBranches.Add(leftBranch);
            }
            else if (UnityEngine.Random.Range(0f, 1f) < DEFAULT_BRANCH_PROBABILITY)
            {
                Segment rightBranch = SegmentBranch(
                    previousSegment, previousSegment.direction + 90f + RandomBranchAngle()
                );
                newBranches.Add(rightBranch);
            }
        }
        
        // make branches not extend beyond area and delete extending non highways

        // remove extending non highways
        var newBranchesFiltered = newBranches.Where(b => {
            return (CheckIfPointInRange(b.start, minCorner, maxCorner) &&
                    CheckIfPointInRange(b.end, minCorner, maxCorner) ||
                    b.metadata.highway) && !checkIfCollisionOrCrossingRiver(b);
        });

        // make extending highways stick to border
        foreach(var b in newBranchesFiltered) {
            if(b.start.x < minCorner.x)
                b.start = new Vector2(minCorner.x, b.start.y);
            if(b.start.x > maxCorner.x)
                b.start = new Vector2(maxCorner.x, b.start.y);
            if(b.start.y < minCorner.y)
                b.start = new Vector2(b.start.x, minCorner.y);
            if(b.start.y > maxCorner.y)
                b.start = new Vector2(b.start.x, maxCorner.y);

            if(b.end.x < minCorner.x)
                b.end = new Vector2(minCorner.x, b.end.y);
            if(b.end.x > maxCorner.x)
                b.end = new Vector2(maxCorner.x, b.end.y);
            if(b.end.y < minCorner.y)
                b.end = new Vector2(b.end.x, minCorner.y);
            if(b.end.y > maxCorner.y)
                b.end = new Vector2(b.end.x, maxCorner.y);
        }

        foreach (var branch in newBranchesFiltered) {
            branch.previousSegmentToLink = previousSegment;
        }

        return newBranchesFiltered.ToList();
    }

    private bool checkIfCollisionOrCrossingRiver(Segment segment)
    {
        float3 pointOnSpline;
        float distance;
        (distance, pointOnSpline) = river.ifRiver(segment.end.x,segment.end.y);
        if (distance < river.riverWidth * 1.6)
        {
            if (segment.metadata.highway)
            {
                Vector2 orginalDirection = segment.end - segment.start;
                Vector2 normalizedDirection = orginalDirection.normalized;
                Vector2 offset = normalizedDirection * (distance + (river.riverWidth * 20f));
                segment.end = segment.end + offset;
                MakeSegmentOnScene(segment);
                return false;
            }
            return true;
        }
        return actionWhenRoadCrossesRiver(pointOnSpline, segment);
    }

    private bool actionWhenRoadCrossesRiver(float3 point, Segment segment)
    {
        float differenceStartX = point.x - segment.start.x;
        float differenceEndX = point.x - segment.end.x;
        if (differenceEndX * differenceStartX < 0)
        {
            return !segment.metadata.highway;
        }
        if(differenceStartX ==0 && differenceEndX == 0)
        {
            return true;
        }
        return false;
    }

    // set randomPopOffset and return max noise value
    private Vector2 getMaxNoisePos() {
        while(true) {
            randomPopOffsetX = UnityEngine.Random.Range(0f, 99999f);
            randomPopOffsetY = UnityEngine.Random.Range(0f, 99999f);

            float lenX = maxCorner.x - minCorner.x;
            float lenY = maxCorner.y - minCorner.y;

            Vector2 minNoiseAccept = new Vector2(minCorner.x + lenX * 0.35f, minCorner.y + lenY * 0.35f);
            Vector2 maxNoiseAccept = new Vector2(minCorner.x + lenX * 0.65f, minCorner.y + lenY * 0.65f);

            int rowsCols = 500;
            float stepRow = lenX / rowsCols;
            float stepCol = lenY / rowsCols;

            float maxNoise = 0f;
            Vector2 maxNoisePos = new Vector2(0f, 0f);

            for(float i = 0f; i <= maxCorner.x; i += stepRow) {
                for(float j = 0f; j <= maxCorner.y; j += stepCol) {
                    float noise = SampleNoise(new Vector2(i, j));
                    if(noise > maxNoise) {
                        maxNoisePos = new Vector2(i, j);
                        maxNoise = noise;
                    }
                }
            }

            if(CheckIfPointInRange(maxNoisePos, minNoiseAccept, maxNoiseAccept)) {
                return maxNoisePos;
            }
        }
    }

    private bool CheckIfPointInRange(Vector2 point, Vector2 min, Vector2 max) {
        return point.x >= min.x && point.x <= max.x &&
                point.y >= min.y && point.y <= max.y;
    }

    private Segment SegmentContinue(Segment previousSegment, float direction) {
        return Segment.CreateUsingDirection(
            previousSegment.end, direction,
            previousSegment.length, 0, previousSegment.metadata
        );
    }
    
    private Segment SegmentBranch(Segment previousSegment, float direction)
    {
        int t = previousSegment.metadata.highway ? NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY : 0;
        return Segment.CreateUsingDirection(
            previousSegment.end, direction,
            DEFAULT_SEGMENT_LENGTH, t, new SegmentMetadata()
        );
    }

    private Segment FindHighwayContinuation(Segment previousSegment) {
        int pointsToSearch = 5;
        float range = HIGHWAY_ANGLE_SEARCH_RANGE * 2;
        float step = range / (pointsToSearch - 1);
        float startDirection = previousSegment.direction + HIGHWAY_ANGLE_SEARCH_RANGE;
        
        float maxNoise = 0f;
        Vector2? highestNoise = null;

        for(int i = 0; i < pointsToSearch; i++) {
            float direction = startDirection - i * step;
            Vector2 p = new Vector2(
                previousSegment.end.x + previousSegment.length * Mathf.Sin(Mathf.Deg2Rad * direction),
                previousSegment.end.y + previousSegment.length * Mathf.Cos(Mathf.Deg2Rad * direction)
            );

            if(SampleNoise(p) > maxNoise) {
                highestNoise = p;
                maxNoise = SampleNoise(p);
            }
        }

        return new Segment(previousSegment.end, (Vector2)highestNoise, 0, previousSegment.metadata);
    }

    public float SamplePopulation(Vector2 start, Vector2 end)
    {
        float s_start = Mathf.PerlinNoise(start.x / maxCorner.x * scale + randomPopOffsetX, start.y / maxCorner.y * scale + randomPopOffsetY);
        float s_end = Mathf.PerlinNoise(end.x / maxCorner.x * scale + randomPopOffsetX, end.y / maxCorner.y * scale + randomPopOffsetY);
        return (s_start + s_end) / 2f;
    }

    public float SampleNoise(Vector2 point) {
        return Mathf.PerlinNoise(point.x / maxCorner.x * scale + randomPopOffsetX, point.y / maxCorner.y * scale + randomPopOffsetY);
    }
}
