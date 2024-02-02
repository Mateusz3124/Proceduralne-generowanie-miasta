using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;
using UnityEngine.Splines;
using System;

public class splineCreation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void createSplines(ProceduralTerrain proceduralTerrain, RoadGen roadGen)
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        foreach (Segment s in roadGen.GenerateSegments(proceduralTerrain.center))
        {
            createSpline(s, proceduralTerrain, splineContainer);
        }
    }
    public void createSpline(Segment segment, ProceduralTerrain proceduralTerrain, SplineContainer splineContainer)
    {
        float heightOffset = 1f;
        Vector2 direction = segment.end - segment.start;
        float length = direction.magnitude;

        List<float3> list = new List<float3>();

        float3 positionFirst = new float3(segment.start.x, proceduralTerrain.getHeight(segment.start.x, segment.start.y) + heightOffset, segment.start.y);
        list.Add(positionFirst);
        //how often there is knot inside spline
        float knotOffset = 40;

        if (length > knotOffset)
        {
            float lengthFraction = knotOffset / length;
            Vector2 pointToAdd = segment.start + ((segment.end - segment.start) * lengthFraction);

            float3 positionInside = new float3(pointToAdd.x, proceduralTerrain.getHeight(pointToAdd.x, pointToAdd.y) + heightOffset, pointToAdd.y);
            list.Add(positionInside);

            int counter = 2;

            while(lengthFraction * counter<1)
            {
                if(counter == 20)
                {
                    Debug.Log("went wrong");
                    break;
                }
                pointToAdd = segment.start + ((segment.end - segment.start) * lengthFraction * counter);
                counter++;
                positionInside = new float3(pointToAdd.x, proceduralTerrain.getHeight(pointToAdd.x, pointToAdd.y) + heightOffset, pointToAdd.y);
                list.Add(positionInside);
            }
        }
        float3 positionLast = new float3(segment.end.x, proceduralTerrain.getHeight(segment.end.x, segment.end.y) + heightOffset, segment.end.y);

        list.Add(positionLast);
        Spline spline = splineContainer.AddSpline();
        spline.Knots = list.Select(x => new BezierKnot(x));
    }
}
