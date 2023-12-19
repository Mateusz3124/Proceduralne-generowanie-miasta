using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class HeatMap
{
    float sample(Vector2 position) {
        return (Mathf.PerlinNoise(position.x, position.y) + 1.0f) * 0.5f;
    }
}
