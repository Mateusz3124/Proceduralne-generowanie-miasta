using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Segment
{
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    public Vector2 start { get; set; }
    public Vector2 end { get; set; }

    private int segmentRevision = 0;
    // time step (priority)
    public int t { get; set; }
    public SegmentMetadata metadata { get; set; }

    // Backwards and forwards links
    public List<Segment> linksB = new List<Segment>();
    public List<Segment> linksF = new List<Segment>();

    public Segment previousSegmentToLink { get; set; }

    public float direction { get; set; }
    private int directionRevision = -1;
    public float length { get; set; }
    private int lengthRevision = -1;

    public void SetStart(Vector2 v)
    {
        start = v;
        segmentRevision++;
    }

    public void SetEnd(Vector2 v)
    {
        end = v;
        segmentRevision++;
    }

    public float GetDirection()
    {
        if (directionRevision != segmentRevision)
        {
            directionRevision = segmentRevision;
            Vector2 vec = end - start;
            direction = -Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg + 90.0f;
        }
        return direction;
    }

    public float GetLength()
    {
        if (lengthRevision != segmentRevision)
        {
            lengthRevision = segmentRevision;
            length = (end - start).magnitude;
        }
        return length;
    }

    public Segment(Vector2 _start, Vector2 _end, int _t, SegmentMetadata _metadata)
    {
        start = _start;
        end = _end;
        t = _t;
        metadata = _metadata;
    }

    public Segment Clone()
    {
        return new Segment(start, end, t, metadata.Clone());
    }

    public void SetupBranchLinks()
    {
        if (previousSegmentToLink == null)
        {
            return;
        }

        // Links between each current branch and each existing branch stemming from the previous segment
        foreach (Segment link in previousSegmentToLink.linksF)
        {
            linksB.Add(link);
            link.LinksForEndContaining(previousSegmentToLink).Add(this);
        }
        previousSegmentToLink.linksF.Add(this);
        linksB.Add(previousSegmentToLink);
    }
    public List<Segment> LinksForEndContaining(Segment segment)
    {
        if (linksB.Contains(segment))
        {
            return linksB;
        }
        else if (linksF.Contains(segment))
        {
            return linksF;
        }
        else
        {
            return null;
        }
    }

    public bool IntersectionWith(Segment other)
    {   
        var point = SegmentsIntersection(start, end, other.start, other.end);
        if (point == null)
        {
            return false;
        }

        if (EqualsApprox(point, start) || EqualsApprox(point, end) ||
            EqualsApprox(point, other.start) || EqualsApprox(point, other.end))
        {
            return false;
        }

        return true;
    }

    private Vector2 SegmentsIntersection(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2) {
        ///
        return null;
    }

    private bool EqualsApprox(Vector2 point1, Vector2 point2) {
        ///
        return true;
    }
}
