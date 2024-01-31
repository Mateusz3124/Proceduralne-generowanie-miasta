using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentMetadata
{
    public bool highway = false;
    public bool severed = false;

    public SegmentMetadata Clone()
    {
        SegmentMetadata clone = new SegmentMetadata
        {
            highway = highway,
            severed = severed
        };

        return clone;
    }
}
