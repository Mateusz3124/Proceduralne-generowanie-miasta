using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGen : MonoBehaviour
{
    const int segment_count_limit = 2000;

    // a segment branching off at a 90 degree angle from an existing segment can vary its direction by +/- this amount
    const float BRANCH_ANGLE_DEVIATION = 3.0f; // degrees
    // a segment continuing straight ahead from an existing segment can vary its direction by +/- this amount
    const float STRAIGHT_ANGLE_DEVIATION = 15.0f; // degrees
    // segments are allowed to intersect if they have a large enough difference in direction - this helps enforce grid-like networks
    const float MINIMUM_INTERSECTION_DEVIATION = 30.0f; // degrees
    // try to produce 'normal' segments with this length if possible
    const int DEFAULT_SEGMENT_LENGTH = 300; // world units
    // try to produce 'highway' segments with this length if possible
    const int HIGHWAY_SEGMENT_LENGTH = 400; // world units
    // each 'normal' segment has this probability of producing a branching segment
    const float DEFAULT_BRANCH_PROBABILITY = 0.4f;
    // each 'highway' segment has this probability of producing a branching segment
    const float HIGHWAY_BRANCH_PROBABILITY = 0.05f;
    // only place 'normal' segments when the population is high enough
    const float NORMAL_BRANCH_POPULATION_THRESHOLD = 0.5f;
    // only place 'highway' segments when the population is high enough
    const float HIGHWAY_BRANCH_POPULATION_THRESHOLD = 0.5f;
    // delay branching from 'highways' by this amount to prevent them from being blocked by 'normal' segments
    const int NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY = 5;
    // allow a segment to intersect with an existing segment within this distance
    const int MAX_SNAP_DISTANCE = 50; // world units

    // select every nth segment to place buildings around - a lower period produces denser building placement
    const int BUILDING_SEGMENT_PERIOD = 5;
    // the number of buildings to generate per selected segment
    const int BUILDING_COUNT_PER_SEGMENT = 10;
    // the maximum distance that a building can be placed from a selected segment
    const float MAX_BUILDING_DISTANCE_FROM_SEGMENT = 400.0f; // world units

    public float RandomBranchAngle()
        {
            return UnityEngine.Random.Range(-BRANCH_ANGLE_DEVIATION, BRANCH_ANGLE_DEVIATION);
        }

    public float RandomStraightAngle()
        {
            return UnityEngine.Random.Range(-STRAIGHT_ANGLE_DEVIATION, STRAIGHT_ANGLE_DEVIATION);
        }

    public List<Segment> GenerateSegments() {
        List<Segment> segments = new List<Segment>();
        PriorityQueue<Segment, int> priorityQueue = new PriorityQueue<Segment, int>();

        // Create the root segment and its opposite direction
        Segment rootSegment = new Segment(new Vector2(0, 0), new Vector2(HIGHWAY_SEGMENT_LENGTH, 0), 0, new SegmentMetadata { highway = true });
        Segment oppositeDirectionSegment = rootSegment.Clone();
        oppositeDirectionSegment.end = new Vector2(rootSegment.start.x - HIGHWAY_SEGMENT_LENGTH, oppositeDirectionSegment.end.y);
        oppositeDirectionSegment.linksB.Add(rootSegment);
        rootSegment.linksB.Add(oppositeDirectionSegment);
        priorityQueue.Push(rootSegment, rootSegment.t);
        priorityQueue.Push(oppositeDirectionSegment, oppositeDirectionSegment.t);

        while (priorityQueue.length > 0 && segments.Count < segment_count_limit)
        {
            // Pop the segment with the smallest t value from the priority queue
            Segment minSegment = priorityQueue.Pop();

            // Check if the segment is accepted by local constraints
            bool isAccepted = LocalConstraints(minSegment, segments);
            if (isAccepted)
            {
                minSegment.SetupBranchLinks();
                //// ps att
                segments.Add(minSegment);

                // Generate new segments based on global goals
                foreach (Segment newSegment in GlobalGoalsGenerate(minSegment))
                {
                    newSegment.t = minSegment.t + 1 + newSegment.t;
                    priorityQueue.Push(newSegment, newSegment.t);
                }
            }
        }

        return segments;
    }

    public bool LocalConstraints(Segment segment, List<Segment> segments)
    {
        Actions action = null;
        int actionPriority = 0;

        List<Segment> matches = new List<Segment>();
        // Get intersections
            ///......
        ////
        foreach (Segment other in matches)
        {
            if (actionPriority <= 4)
            {
                
            }

            if (actionPriority <= 3)
            {
                
            }
            if (actionPriority <= 2)
            {
                
            }
        }

        if (action != null)
        {
            return action.Apply(segment, segments);
        }

        return true;

    }

    private List<Segment> GlobalGoalsGenerate(Segment segment) {

        return null;
    }
}
