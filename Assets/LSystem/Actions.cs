using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Actions
{
    public bool Apply(Segment segment, List<Segment> segments);
}
