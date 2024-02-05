using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SearchService;
using UnityEngine;


public interface Actions
{
    public bool MakeAction(Segment segment, List<Segment> segments);
}

public class LocalConstraintsIntersectionAction : Actions {
    private Segment other;
    private Vector2 intersection;

    public LocalConstraintsIntersectionAction(Segment other, Vector2 intersection)
    {
        this.other = other;
        this.intersection = intersection;
    }

    public bool MakeAction(Segment segment, List<Segment> segments)
    {
        // If intersecting lines are too similar, don't continue
        if (MathUtils.MinDegreeDifference(other.direction, segment.direction) < MathUtils.MINIMUM_INTERSECTION_DEVIATION)
        {
            return false;
        }

        other.Split(intersection, segment, segments);
        segment.end = intersection;
        segment.metadata.severed = true;

        return true;
    }
}

public class LocalConstraintsSnapAction : Actions {
    private Segment other;
    private Vector2 point;

    public LocalConstraintsSnapAction(Segment other, Vector2 point)
    {
        this.other = other;
        this.point = point;
    }

    public bool MakeAction(Segment segment, List<Segment> segments)
    {
        segment.end = point;
        segment.metadata.severed = true;

        // Update links of the other segment
        var links = other.StartIsBackwards() ? other.linksF : other.linksB;

        // Check for duplicate lines, don't add if it exists
        foreach (var link in links)
        {
            if ((link.start.Equals(segment.end) && link.end.Equals(segment.start)) ||
                (link.start.Equals(segment.start) && link.end.Equals(segment.end)))
            {
                return false;
            }
        }

        foreach (var link in links)
        {
            link.LinksForEndContaining(other).Add(segment);
            segment.linksF.Add(link);
        }

        links.Add(segment);
        segment.linksF.Add(other);

        return true;
    }
}

public class LocalConstraintsIntersectionRadiusAction : Actions {

    private Segment other;
    private Vector2 intersection;
    public LocalConstraintsIntersectionRadiusAction(Segment other, Vector2 intersection)
    {
        this.other = other;
        this.intersection = intersection;
    }


    public bool MakeAction(Segment segment, List<Segment> segments)
    {
        segment.end = intersection;
        segment.metadata.severed = true;

        // If intersecting lines are too similar, don't continue
        if (MathUtils.MinDegreeDifference(other.direction, segment.direction) < MathUtils.MINIMUM_INTERSECTION_DEVIATION)
        {
            return false;
        }

        other.Split(intersection, segment, segments);
        return true;
    }


}

public class MathUtils {
    public const float MINIMUM_INTERSECTION_DEVIATION = 30.0f;
    public static float MinDegreeDifference(float d1, float d2)
    {
        float diff = Mathf.Repeat(Mathf.Abs(d1 - d2), 180f);
        return Mathf.Min(diff, Mathf.Abs(diff - 180f));
    }
}