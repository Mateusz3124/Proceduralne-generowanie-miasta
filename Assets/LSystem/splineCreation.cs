using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;
using UnityEngine.Splines;

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
        List<float3> list = new List<float3>();
        float3 position1 = new float3(segment.start.x, proceduralTerrain.getHeight(segment.start.x, segment.start.y), segment.start.y);
        float3 position2 = new float3(segment.end.x, proceduralTerrain.getHeight(segment.end.x, segment.end.y), segment.end.y);
        list.Add(position1);
        list.Add(position2);
        Spline spline = splineContainer.AddSpline();
        spline.Knots = list.Select(x => new BezierKnot(x));
    }
}
