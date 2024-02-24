using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PhysicObjects
{
    public static HashSet<Segment> segmentsColliders = new HashSet<Segment>();
    public static HashSet<Building> buildingsColliders = new HashSet<Building>();
    
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

    public static bool CheckSegmentCollision(Segment segment, Vector2 circleCenter, float radius)
    {
        Vector2 closestPoint = GetClosestPointOnSegment(circleCenter, segment.start, segment.end);

        return Vector2.Distance(closestPoint, circleCenter) < radius;
    }

    public static bool CheckBuildingCollision(Building building, Vector2 circleCenter, float radius) {
        Vector2 localCircleCenter = Quaternion.Euler(0, 0, -building.direction) * (circleCenter - building.center);

        Vector2[] rectangleVertices = new Vector2[4];
        float halfWidth = building.width * 0.5f;
        float halfHeight = building.height * 0.5f;
        rectangleVertices[0] = new Vector2(-halfWidth, -halfHeight);
        rectangleVertices[1] = new Vector2(halfWidth, -halfHeight);
        rectangleVertices[2] = new Vector2(halfWidth, halfHeight);
        rectangleVertices[3] = new Vector2(-halfWidth, halfHeight);

        for (int i = 0; i < 4; i++)
        {
            rectangleVertices[i] = Quaternion.Euler(0, 0, building.direction) * rectangleVertices[i];
        }

        for (int i = 0; i < 4; i++)
        {
            Vector2 sideStart = rectangleVertices[i];
            Vector2 sideEnd = rectangleVertices[(i + 1) % 4];

            Vector2 closestPoint = GetClosestPointOnSegment(localCircleCenter, sideStart, sideEnd);
            if ((closestPoint - localCircleCenter).sqrMagnitude <= radius * radius)
            {
                return true;
            }
        }

        return false;
    }

    public static List<Segment> OverlapCircleSegments(Vector2 circleCenter, float radius) {
        List<Segment> matches = new List<Segment>();
        foreach(Segment segment in segmentsColliders) {
            if(CheckSegmentCollision(segment, circleCenter, radius)) {
                matches.Add(segment);
            }
        }
        return matches;
    }

    public static List<Building> OverlapCircleBuildings(Vector2 circleCenter, float radius) {
        List<Building> matches = new List<Building>();
        foreach(Building building in buildingsColliders) {
            if(CheckBuildingCollision(building, circleCenter, radius)) {
                matches.Add(building);
            }
        }
        return matches;
    }
}
