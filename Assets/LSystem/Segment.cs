using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Segment
{
    private Vector2 _start;
    public Vector2 start 
    { 
        get
        {
            return _start;
        }
        set 
        {
            _start = value;
            direction = CalculateDirection();
            length = (_end - _start).magnitude;
            UpdateCollider();
        } 
    }
    private Vector2 _end;
    public Vector2 end
    { 
        get
        {
            return _end;
        } 
        set 
        {
            _end = value;
            direction = CalculateDirection();
            length = (_end - _start).magnitude;
            UpdateCollider();
        } 
    }

    // time step (priority)
    public int t { get; set; }
    public SegmentMetadata metadata { get; set; }

    // Backwards and forwards links
    public List<Segment> linksB = new List<Segment>();
    public List<Segment> linksF = new List<Segment>();

    public Segment previousSegmentToLink { get; set; }

    public float direction { get; set; }
    public float length { get; set; }


    public Segment(Vector2 _start, Vector2 _end, int _t, SegmentMetadata _metadata)
    {
        this._start = _start;
        this._end = _end;
        t = _t;
        metadata = _metadata;
        length = (_end - _start).magnitude;
        direction = CalculateDirection();
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

        foreach (Segment link in previousSegmentToLink.linksF)
        {
            linksB.Add(link);
            link.LinksForEndContaining(previousSegmentToLink).Add(this);
        }
        previousSegmentToLink.linksF.Add(this);
        linksB.Add(previousSegmentToLink);
    }

    public static Segment CreateUsingDirection(Vector2 start, float direction, float length, int t, SegmentMetadata metadata)
    {
        Vector2 newEnd = new Vector2(
            start.x + length * Mathf.Sin(Mathf.Deg2Rad * direction),
            start.y + length * Mathf.Cos(Mathf.Deg2Rad * direction)
        );
        return new Segment(start, newEnd, t, metadata);
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

    public Vector2? IntersectionWith(Segment other)
    {
        Vector2 point;
        try {
            point = (Vector2)SegmentsIntersection(start, end, other.start, other.end);
        }
        catch {
            // if point is null returns null
            return null;
        }

        if (EqualsApprox(point, start) || EqualsApprox(point, end) ||
            EqualsApprox(point, other.start) || EqualsApprox(point, other.end))
        {
            return null;
        }

        return point;
    }

    public bool StartIsBackwards()
    {
        if (linksB.Count > 0)
        {
            return EqualsApprox(linksB[0].start, start) || EqualsApprox(linksB[0].end, start);
        }
        else if (linksF.Count > 0)
        {
            return EqualsApprox(linksF[0].start, end) || EqualsApprox(linksF[0].end, end);
        }
        else
        {
            return false;
        }
    }

    public Transform CalculatePhysicsShapeTransform()
    {
        float angleInRadians = Mathf.Deg2Rad * -direction;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleInRadians);

        Vector2 middlePoint2d = (start + end) * 0.5f;
        Vector3 middlePoint = new Vector3(middlePoint2d.x, 0f, middlePoint2d.y);

        GameObject transformGameObject = new GameObject("PhysicsShapeTransform");
        Transform resultTransform = transformGameObject.transform;
        resultTransform.position = middlePoint;
        resultTransform.rotation = rotation;
        GameObject.Destroy(transformGameObject);

        return resultTransform;
    }

    public void MakeCollider() 
    {
        // gameObject = new GameObject("RoadCollider");
        // // gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

        // Vector2 direction = end - start;
        // float length = direction.magnitude;

        // Vector2 position = start + 0.5f * direction;
        // gameObject.transform.position = new Vector3(position.x, 0f, position.y);

        // // boxCollider.center = new Vector3(position.x, 0f, position.y);

        // boxCollider.size = new Vector3(SEGMENT_COLLIDER_WIDTH, 0.01f, length);

        // Vector2 transformForward = direction.normalized;
        // boxCollider.transform.forward = new Vector3(transformForward.x, 0f, transformForward.y);

        PhysicObjects.segmentsColliders.Add(this);
    }

    public void DestroyCollider()
    {
        PhysicObjects.segmentsColliders.Remove(this);
    }

    public void UpdateCollider() 
    {
        DestroyCollider();
        MakeCollider();
    }

    public void Split(Vector2 point, Segment segment, List<Segment> segments)
    {
        bool startIsBackwards = StartIsBackwards();
        Segment splitPart = Clone();
        segments.Add(splitPart);
        splitPart.end = point;
        start = point;

        splitPart.linksB = new List<Segment>(linksB);
        splitPart.linksF = new List<Segment>(linksF);

        Segment firstSplit;
        Segment secondSplit;
        List<Segment> fixLinks;
        if (startIsBackwards)
        {
            firstSplit = splitPart;
            secondSplit = this;
            fixLinks = splitPart.linksB;
        }
        else
        {
            firstSplit = this;
            secondSplit = splitPart;
            fixLinks = splitPart.linksF;
        }

        foreach (Segment link in fixLinks)
        {
            int index = link.linksB.FindIndex(s => s == this);
            if (index != -1)
            {
                link.linksB[index] = splitPart;
            }
            else
            {
                index = link.linksF.FindIndex(s => s == this);
                link.linksF[index] = splitPart;
            }
        }

        firstSplit.linksF = new List<Segment> { segment, secondSplit };
        secondSplit.linksB = new List<Segment> { segment, firstSplit };

        segment.linksF.Add(firstSplit);
        segment.linksF.Add(secondSplit);

        UpdateCollider();
        splitPart.MakeCollider();
    }

    private float CalculateDirection() {
        Vector2 vec = end - start;
        return -Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg + 90.0f;
    }

    private Vector2? SegmentsIntersection(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2) {
        float x1 = start1.x, y1 = start1.y, x2 = end1.x, y2 = end1.y;
        float x3 = start2.x, y3 = start2.y, x4 = end2.x, y4 = end2.y;

        float det = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        if (det == 0)
            return null;

        float intersectX = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / det;
        float intersectY = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / det;

        return IsPointOnLine(intersectX, intersectY, x1, y1, x2, y2) &&
               IsPointOnLine(intersectX, intersectY, x3, y3, x4, y4)
            ? new Vector2(intersectX, intersectY)
            : null;
    }

    private bool IsPointOnLine(float x, float y, float x1, float y1, float x2, float y2) {
        return x >= Mathf.Min(x1, x2) && x <= Mathf.Max(x1, x2) && y >= Mathf.Min(y1, y2) && y <= Mathf.Max(y1, y2);
    }

    private bool EqualsApprox(Vector2 point1, Vector2 point2) {
        float epsilon = 0.0001f;
        return Mathf.Abs(point1.x - point2.x) < epsilon && Mathf.Abs(point1.y - point2.y) < epsilon;
    }
}
