using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Control;
using static CreateRegion;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.HableCurve;

public class CreateRegion
{
    private int numberofCellsX = 10;
    private int numberofCellsZ = 10;

    private VoronoiPoint[,] points;

    public enum RegionType
    {
        noroads,
        outskirts,
        lowBuildings,
        highBuildings,
        skyscrapers
    }

    private class VoronoiPoint
    {
        private int numberOfRoads;
        public Vector2 position;
        public RegionType regionType;

        public VoronoiPoint(Vector2 position)
        {
            numberOfRoads = 0;
            this.position = position;
        }
        public void addRoad()
        {
            numberOfRoads++;
        }
        public int getNumberOfRoads()
        {
            return numberOfRoads;
        }
    }

    private float cellSizeX;
    private float cellSizeZ;

    public RegionType getRegion(float x, float z)
    {
        Vector2 position = new Vector2(x, z);
        return findClosestVoronoiPoint(position).regionType;
    }

    private VoronoiPoint findClosestVoronoiPoint(Vector2 position)
    {
        int tileX = (int)Mathf.Floor(position.x / cellSizeX);
        int tileZ = (int)Mathf.Floor(position.y / cellSizeZ);

        float minimalDistance = float.MaxValue;
        VoronoiPoint result = null;

        for (int neighbourX = -1; neighbourX < 2; neighbourX++)
        {
            for (int neighbourZ = -1; neighbourZ < 2; neighbourZ++)
            {
                int currentX = tileX + neighbourX;
                int currentZ = tileZ + neighbourZ;
                if (currentX < 0 || currentX >= numberofCellsX || currentZ < 0 || currentZ >= numberofCellsZ)
                {
                    continue;
                }
                float xDiff = position.x - points[currentX, currentZ].position.x;
                float yDiff = position.y - points[currentX, currentZ].position.y;

                float xDiffSquared = xDiff * xDiff;
                float yDiffSquared = yDiff * yDiff;

                float distance = xDiffSquared + yDiffSquared;
                if (distance < minimalDistance)
                {
                    minimalDistance = distance;
                    result = points[currentX, currentZ];
                }
            }
        }
        return result;
    }

    private bool checkForDenseRoads(int x, int z)
    {
        if (points[x, z].regionType == RegionType.skyscrapers || points[x, z].regionType == RegionType.highBuildings)
        {
            return true;
        }
        return false;
    }

    private bool checkIfSkycraper(int x, int z)
    {
        int count = 0;

        for (int neighbourX = -1; neighbourX < 2; neighbourX++)
        {
            for (int neighbourZ = -1; neighbourZ < 2; neighbourZ++)
            {
                int currentX = x + neighbourX;
                int currentZ = z + neighbourZ;
                if (currentX < 0 || currentX >= numberofCellsX || currentZ < 0 || currentZ >= numberofCellsZ)
                {
                    count++;
                }
                else if (checkForDenseRoads(currentX, currentZ))
                {
                    count++;
                }
                else
                {
                    return false;
                }
            }
        }
        return count == 9;
    }
    public void createRegions(ProceduralTerrain proceduralTerrain, List<Segment> segmentList)
    {
        points = new VoronoiPoint[numberofCellsX, numberofCellsZ];
        cellSizeX = proceduralTerrain.borderX / numberofCellsX;
        cellSizeZ = proceduralTerrain.borderZ / numberofCellsZ;
        for (int x = 0; x < numberofCellsX; x++)
        {
            for (int z = 0; z < numberofCellsZ; z++)
            {
                float randomizedX = UnityEngine.Random.Range(cellSizeX * x, cellSizeX * x + cellSizeX);
                float randomizedZ = UnityEngine.Random.Range(cellSizeZ * z, cellSizeZ * z + cellSizeZ);
                points[x, z] = new VoronoiPoint(new Vector2(randomizedX, randomizedZ));
            }
        }
        foreach (Segment segment in segmentList)
        {
            if (segment.length > 5f)
            {
                findClosestVoronoiPoint(segment.end).addRoad();
            }
        }
        for (int x = 0; x < numberofCellsX; x++)
        {
            for (int z = 0; z < numberofCellsZ; z++)
            {
                int value = points[x, z].getNumberOfRoads();
                if (value == 0)
                {
                    points[x, z].regionType = RegionType.noroads;
                }
                else if (value < 5)
                {
                    points[x, z].regionType = RegionType.outskirts;
                }
                else if (value < 12)
                {
                    points[x, z].regionType = RegionType.lowBuildings;
                }
                else
                {
                    points[x, z].regionType = RegionType.highBuildings;
                }
            }
        }
        for (int x = 0; x < numberofCellsX; x++)
        {
            for (int z = 0; z < numberofCellsZ; z++)
            {
                if (checkIfSkycraper(x,z))
                {
                    points[x, z].regionType = RegionType.skyscrapers;
                }
            }
        }

        return;
        /*
        float sizeCube = proceduralTerrain.borderX / 250;

        for (int x = 0; x < 250; x++)
        {
            for (int z = 0; z < 250; z++)
            {
                GameObject cuber = GameObject.CreatePrimitive(PrimitiveType.Cube);

                float2 center = new float2((sizeCube / 2) + (sizeCube * x), (sizeCube / 2) + (sizeCube * z));

                cuber.transform.position = new Vector3(center.x, 100f, center.y);
                cuber.transform.localScale = new Vector3(sizeCube, 1, sizeCube);
                var regionType = getRegion(center.x, center.y);
                switch (regionType)
                {
                    case RegionType.noroads:
                        cuber.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
                        break;
                    case RegionType.outskirts:
                        cuber.GetComponent<Renderer>().material.color = new Color(0, 255, 255);
                        break;
                    case RegionType.lowBuildings:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 136, 0);
                        break;
                    case RegionType.highBuildings:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
                        break;
                    case RegionType.skyscrapers:
                        cuber.GetComponent<Renderer>().material.color = new Color(255, 0, 255);
                        break;
                    default:
                        break;

                }
            }
        }
        */
    }
}
