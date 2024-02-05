using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PhysicObjects
{
    public static HashSet<Segment> segmentsColliders = new HashSet<Segment>();
    
    // s1, s2 - start and end (or end and start) of segment
    public static Vector2 GetClosestPointOnSegment(Vector2 point, Vector2 s1, Vector2 s2)
    {
        Vector2 segmentDirection = s2 - s1;
        float segmentLengthSquared = segmentDirection.sqrMagnitude;

        if (segmentLengthSquared == 0f)
            return s1; // s1 and s2 are the same point

        Vector2 toPoint = point - s1;
        float t = Mathf.Clamp01(Vector2.Dot(toPoint, segmentDirection) / segmentLengthSquared);

        return s1 + t * segmentDirection;
    }
    
    public static bool IsPointInSegmentRange(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 vec = segmentEnd - segmentStart;
        float dot = Vector2.Dot(point - segmentStart, vec);
        return dot >= 0 && dot <= vec.sqrMagnitude;
    }

    public static bool CheckCollision(Segment segment, Vector2 circleCenter, float radius)
    {
        Vector2 closestPoint = GetClosestPointOnSegment(segment.start, segment.end, circleCenter);

        return Vector2.Distance(closestPoint, circleCenter) < radius;
    }

    public static List<Segment> OverlapCircleSegments(Vector2 circleCenter, float radius) {
        List<Segment> matches = new List<Segment>();
        foreach(Segment segment in segmentsColliders) {
            if(CheckCollision(segment, circleCenter, radius)) {
                matches.Add(segment);
            }
        }
        return matches;
    }
}
