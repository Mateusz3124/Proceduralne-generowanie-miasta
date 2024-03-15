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
using static river_Control;
using System.Buffers.Text;
using static UnityEngine.Rendering.DebugUI;

public class river_Control : MonoBehaviour
{
    public class Square
    {
        public float left;
        public float top;
        public float right;
        public float bottom;
        public Square(float left, float top, float right, float bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;  
        }
    }
    public class Squares
    {
        public List<Square> squares = new List<Square>();

        public void add(float3 start, float3 end)
        {
            float distanceZ = end.z - start.z;
            float distanceX = end.x - start.x;

            float middleZ = start.z + distanceZ / 2;
            float middleX = start.x + distanceX / 2;
            float left = start.x + distanceX / 2 - distanceZ / 2;
            float right = start.x + distanceX / 2 + distanceZ / 2;

            Square square = new Square(left, end.z, right, start.z);
            squares.Add(square);
        }

        public bool checkIfInside(float x, float z)
        {
            bool result = false;
            foreach (var item in squares)
            {
                if (item.left < x && x < item.right)
                {
                    if(item.bottom < z && z < item.top)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
    }

    Squares squares = new Squares();

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
    float checkForRiver(float x, float z)
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

    public void createRandomRiver()
    {
        SplineContainer splineContainer = river.GetComponent<SplineContainer>();
        Spline splineTemporary = splineContainer.AddSpline();
        float startX = 0;
        int gap = proceduralTerrain.size / 6;

        List<float3> listForSplineTemporary = new List<float3>();
        
        xOffset = proceduralTerrain.size / 20;
        for (float z = proceduralTerrain.GetMinCorner().y; z <= proceduralTerrain.GetMaxCorner().y; z += gap)
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
        float3 firstKnot = splineTemporary.ToArray()[0].Position;
        float firstheightTerrain = proceduralTerrain.getHeight(firstKnot.x, firstKnot.z) - 4f;
        listForAccurateSpline.Add(new float3(firstKnot.x, firstheightTerrain, firstKnot.z));

        for (int i = 0; i < length; i+=200)
        {
            float3 pointOnSpline = SplineUtility.GetPointAtLinearDistance<Spline>(splineTemporary, resultPoint, 200f, out resultPoint);

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

        for(int i=0; i < 10; i++)
        {
            float ratioStart = i / 10f;
            float ratioEnd = (i+1) / 10f;
            float3 pointOnSplineStart = spline.EvaluatePosition(ratioStart);
            float3 pointOnSplineEnd = spline.EvaluatePosition(ratioEnd);
            squares.add(pointOnSplineStart, pointOnSplineEnd);
        }

        river_mesh river_Mesh = river.GetComponent<river_mesh>();
        river_Mesh.CreateMesh(spline, riverWidth);
        return;
    }

    public void riverToTerrain()
    {
        flatten_for_river flatten = GetComponent<flatten_for_river>();
        flatten.PerimeterRampDistance = riverWidth/2;
        List<Point> list = new List<Point>();
        List<Square> localSquares = squares.squares;
        float resolutionDistance = proceduralTerrain.size / proceduralTerrain.resolution;
        foreach(Square square in localSquares)
        {
            list.Clear();
            int startX = (int)Mathf.Floor(square.left / resolutionDistance);
            int endX = (int)Mathf.Floor(square.right / resolutionDistance);
            int startZ = (int)Mathf.Floor(square.bottom / resolutionDistance);
            int endZ = (int)Mathf.Floor(square.top / resolutionDistance);
            var filter = GetComponent<MeshFilter>();
            Vector3[] verticies = filter.mesh.vertices;
            for (int x = startX; x <= endX; x++)
            {
                for(int z = startZ; z <= endZ; z++)
                {
                    float height = (checkForRiver(x* resolutionDistance,z * resolutionDistance) - 0.1f);
                    if (height > 0f)
                    {
                        Point point = new Point(new int2(x, z));
                        point.height = height;
                        list.Add(point);
                    }
                }
            }

            flatten.changeTerrain(terrains[1, 1], list);
        } 

    }
    public (float,float3, bool) ifRiver(float x, float y)
    {
        if (!squares.checkIfInside(x, y)){
            return (0f, new float3(0f,0f,0f), false);
        }
        Vector3 rayOrigin = new float3(x, -1f, y);
        Ray ray = new Ray(rayOrigin, Vector3.up);
        float3 pointOnSpline;
        float interpolation;
        float distance = SplineUtility.GetNearestPoint<Spline>(spline, ray, out pointOnSpline, out interpolation, SplineUtility.PickResolutionMin);
        return (distance,pointOnSpline, true);
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

