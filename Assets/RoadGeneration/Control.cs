using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class Control : MonoBehaviour
{
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    // Start is called before the first frame update
    private ProceduralTerrain proceduralTerrain;
    public SplineMesh sm;

    public enum regionType
    {
        noroads,
        rarelyRoads,
        averageNumberOfRoads,
        denseRoads,
        skyscraper
    }
    void Start()
    {
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.generate();

        RoadGen roadGen = GetComponent<RoadGen>();
        roadGen.minCorner = new Vector2(0f, 0f);
        roadGen.maxCorner = new Vector2(proceduralTerrain.borderX, proceduralTerrain.borderZ);


        var list = roadGen.GenerateSegments(proceduralTerrain.center);

        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, list);
        sm.CreateMesh(GetComponent<SplineContainer>(), transform);

        //river_Control river = GetComponent<river_Control>();
        //river._proceduralTerrain = proceduralTerrain;
        //river.riverToTerrain(river.createRiver());

        int[,] cube = new int[10,10];

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                cube[i, j] = 0;
            }
        }

        float value = proceduralTerrain.borderX / 10;

        foreach (Segment segment in list)
        {
            if (segment.length > 10) {
                int tileX = (int)Math.Floor(segment.end.x / value);
                int tileZ = (int)Math.Floor(segment.end.y / value);
                cube[tileX, tileZ]++;
            }
        }

        regionType[,] regions = new regionType[10,10];
        List <Vector2> poly = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                int valuer = cube[i, j];
                if (valuer == 0)
                {
                    regions[i, j] = regionType.noroads;
                }
                else if (valuer < 5)
                {
                    regions[i, j] = regionType.rarelyRoads;
                }
                else if (valuer < 12)
                {
                    regions[i, j] = regionType.averageNumberOfRoads;
                }
                else
                {
                    regions[i, j] = regionType.denseRoads;
                    poly.Add(new Vector2(i, j));
                }
            }
        }
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                try
                {
                    var value1 = regions[i-1,j];
                    var value2 = regions[i+1, j];
                    var value3 = regions[i, j - 1];
                    var value4 = regions[i, j + 1];

                    bool condition = ((value1 == regionType.denseRoads) || (value1 == regionType.skyscraper)) && ((value2 == regionType.denseRoads) || (value2 == regionType.skyscraper)) && ((value3 == regionType.denseRoads) || (value3 == regionType.skyscraper)) && ((value4 == regionType.denseRoads) || (value4 == regionType.skyscraper));
                    if (condition)
                    {
                        regions[i, j] = regionType.skyscraper;
                    }
                }
                catch {
                    continue;
                }
            }
        }


        for (int x = 0; x < proceduralTerrain.borderX / value; x++)
        {
            for (int z = 0; z < proceduralTerrain.borderZ / value; z++)
            {
                GameObject cuber = GameObject.CreatePrimitive(PrimitiveType.Cube);

                float2 center = new float2((value / 2) + (value * x), (value / 2) + (value * z));

                cuber.transform.position = new Vector3(center.x, 100f, center.y);
                cuber.transform.localScale = new Vector3(value, 1, value);
                var regionType = regions[x, z];
                switch (regionType)
                {
                    case regionType.noroads:
                        cuber.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
                        break;
                    case regionType.rarelyRoads:
                        cuber.GetComponent<Renderer>().material.color = new Color(0, 255, 255);
                        break;
                    case regionType.averageNumberOfRoads:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 136, 0);
                        break;
                    case regionType.denseRoads:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
                        break;
                    case regionType.skyscraper:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 0, 255);
                        break;
                    default:
                        break;

                }
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
