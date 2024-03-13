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

public class river_Control : MonoBehaviour
{
    /*
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private int height;
    private int width;
    private GameObject[,] terrains;
    [HideInInspector]
    private ProceduralTerrain proceduralTerrain;
    public ProceduralTerrain _proceduralTerrain
    {
        get { return proceduralTerrain; }
        set { 
            proceduralTerrain = value;
            height = value.height; 
            width = value.width;
            terrains = value.terrains;
        }
    }
    public GameObject river;
    [HideInInspector]
    public int riverWidth;
    public Spline spline;
    public List<int2> createRiver()
    {
        List<int2> values = new List<int2>();
        List<float3> list = new List<float3>();
        
        for (int i = 0; i < 20* height; i++)
        {
            for (int z = 0; z < riverWidth; z++)
            {
                values.Add(new int2(width*10+200+z, i));
            }
            float positionX = width * 10 + 200f + riverWidth/2;
            float height = proceduralTerrain.getHeight(positionX, i) - 4f+100f;
            height = height > 0.3f ? height : 0.3f;
            list.Add(new float3(positionX, height, i));
        }
        SplineContainer splineContainer = river.GetComponent<SplineContainer>();

        spline = splineContainer.AddSpline();
        spline.Knots = list.Select(x => new BezierKnot(x));
        spline.SetTangentMode(TangentMode.AutoSmooth);
        river_mesh river_Mesh = river.GetComponent<river_mesh>();
        river_Mesh.CreateMesh(spline, riverWidth+20f);
        return values;
    }
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
        float startX = width * proceduralTerrain.numberOfTilesX / 2 + width/2;
        int gap = proceduralTerrain.borderZ / 6 -1;

        List<float3> listForSplineTemporary = new List<float3>();
        
        HashSet<int2> tilesValuesForFlatten = new HashSet<int2>();

        for (int z =0; z < proceduralTerrain.borderZ; z += gap)
        {
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand < 0.2f && startX+width < proceduralTerrain.borderX) startX += width;
            if (rand > 0.8f && startX-width >= 0) startX -= width;
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
            int tileX = (int)Mathf.Floor(pointOnSpline.x / width);
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
    public void testRiver()
    {
        int sizeOfGrid = proceduralTerrain.borderX / 300;
        float[,] gridOfHeightsAverage = new float[300,300];
        for (int x = 0; x < 300; x++)
        {
            for (int z = 0; z < 300; z++)
            {
                float minValue = sizeOfGrid*x;
                float maxValue = sizeOfGrid*x+sizeOfGrid;
                float randomSamplesSum = new float();
                for(int i = 0; i < 3; i++)
                {
                    float chosenX = UnityEngine.Random.Range(minValue, maxValue);
                    float chosenZ = UnityEngine.Random.Range(minValue, maxValue);
                    randomSamplesSum += proceduralTerrain.getHeight(chosenX, chosenZ);
                }
                gridOfHeightsAverage[x,z] = randomSamplesSum/3;
            }
        }
        findPath(gridOfHeightsAverage, new int2(0, 25), new int2(299, 200));
    }
    private bool[,] visited;
    private float[,] dist;
    private int2[,] prev;
    public void findPath(float[,] graph,int2 start, int2 end)
    {
        int number = graph.GetLength(0);
        visited = new bool[number, number];
        dist = new float[number, number];
        prev = new int2[number, number];
        for (int x = 0; x < number; x++)
        {
            for (int z = 0; z < number; z++)
            {
                dist[x, z] = float.MaxValue;
                visited[x, z] = false;
            }
        }
        dist[start.x, start.y] = 0;
        visited[start.x, start.y] = true;
        prev[start.x, start.y].x = -1;
        prev[start.x, start.y].y = -1;
        PriorityQueue<int2, float> queue = new PriorityQueue<int2, float>();
        queue.Push(start, 0f);

        while (queue.length != 0)
        {
            int2 currentNode = queue.Pop();
            int x = currentNode.x;
            int z = currentNode.y;
            int left = x - 1;
            int top = z - 1;
            int right = x + 1;
            int bottom = z + 1;
            visited[x, z] = true;
            if (left >= 0 && !visited[left, z])
            {
                float value = dist[x, z] + graph[left, z];
                if (value < dist[left, z])
                {
                    dist[left, z] = value;
                    prev[left, z].x = x;
                    prev[left, z].y = z;
                }
                int2 valuer = new int2(left, z);
                if (!queue.Contains(valuer))
                    queue.Push(new int2(left, z), dist[left,z]);
            }
            if (top >= 0 && !visited[x, top])
            {
                float value = dist[x, z] + graph[x, top];
                if (value < dist[x, top])
                {
                    dist[x, top] = value;
                    prev[x, top].x = x;
                    prev[x, top].y = z;
                }
                int2 valuer = new int2(x, top);
                if (!queue.Contains(valuer))
                    queue.Push(new int2(x, top), dist[x,top]);
            }
            if (right < number && !visited[right, z])
            {
                float value = dist[x, z] + graph[right, z];
                if (value < dist[right, z])
                {
                    dist[right, z] = value;
                    prev[right, z].x = x;
                    prev[right, z].y = z;
                }
                int2 valuer = new int2(right, z);
                if (!queue.Contains(valuer))
                    queue.Push(new int2(right, z), dist[right,z]);
            }
            if (bottom < number && !visited[x, bottom])
            {
                float value = dist[x, z] + graph[x, bottom];
                if (value < dist[x, bottom])
                {
                    dist[x, bottom] = value;
                    prev[x, bottom].x = x;
                    prev[x, bottom].y = z;
                }
                int2 valuer = new int2(x, bottom);
                if(!queue.Contains(valuer))
                    queue.Push(valuer, dist[x,bottom]);
            }
        }
        createCube(end.x, end.y);
        int2 current = new int2(prev[end.x, end.y].x, prev[end.x, end.y].y);
        createCube(current.x, current.y);
        while (current.x != -1 && current.y != -1)
        {
            current = prev[current.x,current.y];
            if(current.x == -1)
            {
                return;
            }
            createCube(current.x, current.y);
        }
    }
    public void createCube(int x, int z)
    {
        GameObject cuber = GameObject.CreatePrimitive(PrimitiveType.Cube);
        int sizeCube = proceduralTerrain.borderX / 300;
        float2 center = new float2((sizeCube / 2) + (sizeCube * x), (sizeCube / 2) + (sizeCube * z));
        cuber.transform.position = new Vector3(center.x, 100f, center.y);
        cuber.transform.localScale = new Vector3(sizeCube, 1, sizeCube);
        cuber.GetComponent<Renderer>().material.color = Color.red;
    }
    public void riverToTerrain(HashSet<int2> tiles)
    {
        flatten_for_river flatten = GetComponent<flatten_for_river>();
        flatten.PerimeterRampDistance = riverWidth/2;
        List<Point> list = new List<Point>();
        foreach(int2 value in tiles)
        {
            list.Clear();
            int baseX = value.x * width;
            int baseZ = value.y * height;
            for (int x = 0; x< width; x++)
            {
                for(int z = 0; z < height; z++)
                {
                    float height = (checkForRiver(baseX + x, baseZ + z)-0.1f)/proceduralTerrain.depth;
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

}

public class Point
{
    public int2 position;
    public float height;
    public Point(int2 position)
    {
        this.position = position;
    }    */
}

