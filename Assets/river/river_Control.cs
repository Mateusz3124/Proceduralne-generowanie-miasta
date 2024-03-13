using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using System;
using JetBrains.Annotations;
using System.Runtime.ConstrainedExecution;
using UnityEditor.Presets;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HableCurve;

public class river_Control : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private int height;
    private float xOffset;
    private GameObject[,] terrains;
    [HideInInspector]
    private ProceduralTerrain proceduralTerrain;
    public ProceduralTerrain _proceduralTerrain
    {
        get { return proceduralTerrain; }
        set { 
            proceduralTerrain = value;
            height = value.height; 
        }
    }
    public GameObject river;
    [HideInInspector]
    public int riverWidth;

    public Spline spline;
    float checkForRiver(int x, int z)
    {
        RaycastHit hit;
        int layerMask = 1 << 4;
        Vector3 positionVector = new Vector3(x,200f,z);
        Ray rayDown = new Ray(positionVector, Vector3.down);
        if (Physics.Raycast(rayDown, out hit, 200, layerMask))
        {
            // detect if road
            if (hit.collider != null && hit.collider.GetType() == typeof(MeshCollider))
            {
                return hit.point.y;
            }
        }
        return -1f;
    }
    public HashSet<int2> createRandomRiver()
    {
        SplineContainer splineContainer = river.GetComponent<SplineContainer>();
        Spline splineTemporary = splineContainer.AddSpline();
        float startX = 0;
        int gap = proceduralTerrain.size / 6;

        List<float3> listForSplineTemporary = new List<float3>();
        
        HashSet<int2> tilesValuesForFlatten = new HashSet<int2>();
        xOffset = proceduralTerrain.size / 20;
        for (int z = (int)proceduralTerrain.GetMinCorner().y; z <= proceduralTerrain.GetMaxCorner().y; z += gap)
        {
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand < 0.2f && startX+xOffset < proceduralTerrain.GetMaxCorner().x) startX += xOffset;
            if (rand > 0.8f && startX-xOffset >= proceduralTerrain.GetMinCorner().x) startX -= xOffset;
            float height = 5f;
            listForSplineTemporary.Add(new float3(startX, height, z));
        }
        splineTemporary.Knots = listForSplineTemporary.Select(x => new BezierKnot(x));
        splineTemporary.SetTangentMode(TangentMode.AutoSmooth);

        float resultPoint = 0f;
        float length = splineContainer.CalculateLength();
        List<float3> listForAccurateSpline = new List<float3>();

        for (int i = 0; i < length; i+=10)
        {
            float3 pointOnSpline = SplineUtility.GetPointAtLinearDistance<Spline>(splineTemporary, resultPoint, 10f, out resultPoint);
            int tileX = (int)Mathf.Floor(pointOnSpline.x / xOffset);
            int tileZ = (int)Mathf.Floor(pointOnSpline.z / height);

            tilesValuesForFlatten.Add(new int2(tileX, tileZ));

            float heightTerrain = proceduralTerrain.getHeight(pointOnSpline.x, pointOnSpline.z) -4f;
            heightTerrain -= heightTerrain * 0.2f;
            if (heightTerrain < 0.3f)
            {
                heightTerrain = 0.3f;
            }
            listForAccurateSpline.Add(new float3(pointOnSpline.x, heightTerrain, pointOnSpline.z));
        }

        spline = splineContainer.AddSpline();
        spline.Knots = listForAccurateSpline.Select(x => new BezierKnot(x));
        spline.SetTangentMode(TangentMode.AutoSmooth);
        splineContainer.RemoveSpline(splineTemporary);

        river_mesh river_Mesh = river.GetComponent<river_mesh>();
        river_Mesh.CreateMesh(spline, riverWidth);
        return tilesValuesForFlatten;
    }

    public void riverToTerrain(HashSet<int2> tiles)
    {
        flatten_for_river flatten = GetComponent<flatten_for_river>();
        flatten.PerimeterRampDistance = riverWidth/2;
        List<Point> list = new List<Point>();
        foreach(int2 value in tiles)
        {
            list.Clear();
            int baseX = value.x * (int)xOffset;
            int baseZ = value.y * height;
            for (int x = 0; x< xOffset; x++)
            {
                for(int z = 0; z < height; z++)
                {
                    float height = (checkForRiver(baseX + x, baseZ + z)-0.1f);
                    if(height > 0f)
                    {
                        Point point = new Point(new int2(x,z));
                        point.height = height;
                        list.Add(point);
                    }
                }
            }
            flatten.changeTerrain(terrains[value.x, value.y], list);
        }

    }
    public (float,float3) ifRiver(float x, float y)
    {
        Vector3 rayOrigin = new float3(x, -1f, y);
        Ray ray = new Ray(rayOrigin, Vector3.up);
        float3 pointOnSpline;
        float interpolation;
        float distance = SplineUtility.GetNearestPoint<Spline>(spline, ray, out pointOnSpline, out interpolation, SplineUtility.PickResolutionMin);
        return (distance,pointOnSpline);
    }

}

public class Point
{
    public int2 position;
    public float height;
    public Point(int2 position)
    {
        this.position = position;
    } 
}

