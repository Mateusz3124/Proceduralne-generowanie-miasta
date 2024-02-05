using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoadGen : MonoBehaviour
{
    public int segment_count_limit = 2000;

    const float BRANCH_ANGLE_DEVIATION = 3.0f; // degrees
    const float STRAIGHT_ANGLE_DEVIATION = 15.0f; // degrees
    const int HIGHWAY_SEGMENT_LENGTH = 400;
    const float DEFAULT_BRANCH_PROBABILITY = 0.4f;
    const float HIGHWAY_BRANCH_PROBABILITY = 0.05f;
    // Branches are made based on perlin noise
    // Higher noise value, more roads are there
    const float NORMAL_BRANCH_POPULATION_THRESHOLD = 0.5f;
    const float HIGHWAY_BRANCH_POPULATION_THRESHOLD = 0.5f;
    // Snap action reach
    const int MAX_SNAP_DISTANCE = 50;
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    const int NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY = 5;
    const int DEFAULT_SEGMENT_LENGTH = 300;

    // borders to lsystem
    // has default values but its better to set manually
    public Vector2 minCorner {get; set;} = new Vector2(0f, 0f);
    public Vector2 maxCorner {get; set;} = new Vector2(512f, 512f);


    void Start() 
    {

    }

    public void MakeSegmentOnScene(Segment segment) 
    {
        GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Vector2 direction = segment.end - segment.start;
        float length = direction.magnitude;

        Vector2 positionVec = segment.start + 0.5f * direction;
        segmentObject.transform.position = new Vector3(positionVec.x, 0f, positionVec.y);

        segmentObject.transform.localScale = new Vector3(SEGMENT_COLLIDER_WIDTH, 0.01f, length);

        Vector2 directionVector = direction.normalized;
        segmentObject.transform.forward = new Vector3(directionVector.x, 0f, directionVector.y);
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
        List<Segment> segments = new List<Segment>();
        PriorityQueue<Segment, int> priorityQueue = new PriorityQueue<Segment, int>();
        // Main segment and opposite direction segment
        Segment mainSegment = new Segment(
            center, new Vector2(center.x + HIGHWAY_SEGMENT_LENGTH, center.y),
            0, new SegmentMetadata { highway = true });
        Segment oppositeDirectionSegment = new Segment(
            center, new Vector2(center.x-HIGHWAY_SEGMENT_LENGTH, center.y),
            0, new SegmentMetadata { highway = true });
        oppositeDirectionSegment.linksB.Add(mainSegment);
        mainSegment.linksB.Add(oppositeDirectionSegment);
        priorityQueue.Push(mainSegment, mainSegment.t);
        priorityQueue.Push(oppositeDirectionSegment, oppositeDirectionSegment.t);

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
        List<Segment> matches = new List<Segment>();

        // Check collision around segment
        Vector3 pos3d = segment.CalculatePhysicsShapeTransform().position;
        List<Segment> colliders = PhysicObjects.OverlapCircleSegments(new Vector2(pos3d.x, pos3d.z),
                                                             segment.length * 0.5f + MAX_SNAP_DISTANCE);
        foreach (Segment collidingSegment in colliders)
        {
            if (collidingSegment != null)
            {
                matches.Add(collidingSegment);
            }
        }

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

        if (!previousSegment.metadata.severed)
        {
            Segment continueStraight = SegmentContinue(previousSegment, previousSegment.direction);
            float straightPop = SamplePopulation(continueStraight.start, continueStraight.end);

            if (previousSegment.metadata.highway)
            {
                float randomStraightAngle = RandomStraightContinueAngle();
                Segment randomStraight = SegmentContinue(
                    previousSegment, previousSegment.direction + randomStraightAngle
                );
                float randomPop = SamplePopulation(randomStraight.start, randomStraight.end);
                float roadPop;
                if (randomPop > straightPop)
                {
                    newBranches.Add(randomStraight);
                    roadPop = randomPop;
                }
                else
                {
                    newBranches.Add(continueStraight);
                    roadPop = straightPop;
                }
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
        } 

        // filter branches that extend beyond the boundaries of the area
        var newBranchesFiltered = newBranches.Where(b => { 
            return b.start.x >= minCorner.x && b.start.x < maxCorner.x &&
                    b.start.y >= minCorner.y && b.start.y < maxCorner.y &&
                b.end.x >= minCorner.x && b.end.x < maxCorner.x &&
                    b.end.y >= minCorner.y && b.end.y < maxCorner.y;
        });
        
        foreach (var branch in newBranchesFiltered)
        {
            branch.previousSegmentToLink = previousSegment;
        }

        return newBranchesFiltered.ToList();
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

    private float SamplePopulation(Vector2 start, Vector2 end)
    {
        float s_start = (Mathf.PerlinNoise(start.x, start.y) + 1.0f) / 2f;
        float s_end = (Mathf.PerlinNoise(end.x, end.y) + 1.0f) / 2f;
        return (s_start + s_end) / 2f;
    }
}
