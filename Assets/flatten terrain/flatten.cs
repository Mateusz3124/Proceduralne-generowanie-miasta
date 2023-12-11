using UnityEngine;
using UnityEngine.TerrainTools;
using Unity.Mathematics;
using System;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;

public class Flatten : MonoBehaviour
{
    public int startX = 1;
    public int endX = 2;
    public int startZ = 10;
    public int endZ = 12;
    public float targetHeight = 20f;
    public RoadGenerator rg;
    public ProceduralTerrain proceduralTerain;
    private void Start()
    {

    }
    public void flattenBasedOnPoint(List<float3> positions, float streetWidth)
    {
        proceduralTerain = rg.GetTerrain();
        List<float3> data = new List<float3>(positions);
        data.Sort((a, b) => -a.y.CompareTo(b.y));
        foreach (float3 position in data)
        {
            int width = Mathf.FloorToInt(streetWidth);
            //check if position is on side or corner of square
            bool left = position.x - Math.Floor(position.x) == 0;
            bool right = position.x - Math.Ceiling(position.x) == 0;
            bool down = position.z - Math.Floor(position.z) == 0;
            bool top = position.z - Math.Ceiling(position.z) == 0;

            if (left || right || down || top)
            {
                isInsideEdge(position, width, left, right, top, down);
            }
            else
            {
                isInsideSquare(position, width);
            }

            //very simplified solution for not straight for now
            //isInsideSquare(position, width);
        }
    }

    public void isInsideSquare(Vector3 position, int width)
    {
        int leftBottomX = Mathf.FloorToInt(position.x);
        int leftBottomZ = Mathf.FloorToInt(position.z);

        // Mathf.Floor -> round down Mathf.Ceil -> round up           
        int4 squareInWhichIsPoint = new int4(leftBottomX, leftBottomZ, leftBottomX + 1, leftBottomZ +1);
        SetTerrainHeightForArea(squareInWhichIsPoint, position.y);
    }
    public void isInsideEdge(Vector3 position, int width, bool left, bool right, bool top, bool down)
    {
        int leftBottomX = Mathf.FloorToInt(position.x);
        int leftBottomZ = Mathf.FloorToInt(position.z);
        int4 squareToFlatten = new int4();
        // check if inside corner
        if ( (left && down && top && right))
        {
            squareToFlatten = new int4((int)position.x-1, (int)position.z-1, (int)position.x + 1, (int)position.z + 1);
        }

        else if (left)
        {
            squareToFlatten = new int4((int)position.x - 1, (int)position.z, (int)position.x + 1, (int)position.z + 1);
        }

        else if (right)
        {
            squareToFlatten = new int4((int)position.x, (int)position.z, (int)position.x + 2, (int)position.z + 1);
        }

        else if (top)
        {
            squareToFlatten = new int4((int)position.x, (int)position.z, (int)position.x + 1, (int)position.z + 2);
        }

        else if (down)
        {
            squareToFlatten = new int4((int)position.x, (int)position.z+1, (int)position.x + 1, (int)position.z - 1);
        }
        Vector3 signPosition = new Vector3(position.x, 0, position.z);
        //simpleSetTerrainHeightForSquare(squareToFlatten, proceduralTerain.terrain.SampleHeight(signPosition)/proceduralTerain.depth);
        simpleSetTerrainHeightForSquare(squareToFlatten, proceduralTerain.GetHeight(leftBottomZ, leftBottomX));
    }
    //simplified version just for now
    public void simpleSetTerrainHeightForSquare(int4 area, float targetHeight)
    {
        TerrainData terrainData = proceduralTerain.terrain.terrainData;
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        startX = Mathf.Clamp(area[0], 0, terrainData.heightmapResolution - 1);
        endX = Mathf.Clamp(area[2], 0, terrainData.heightmapResolution - 1);
        startZ = Mathf.Clamp(area[1], 0, terrainData.heightmapResolution - 1);
        endZ = Mathf.Clamp(area[3], 0, terrainData.heightmapResolution - 1);
        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                // why this is inverse who knows it just work
                heights[z, x] = targetHeight;
            }
        }
        terrainData.SetHeights(0, 0, heights);

    }
    // soon to be implemented for now looks good
    public void SetTerrainHeightForArea(int4 area, float targetHeight)
    {
        //area looks like this -> [startx,startz,endx,endz]

        // Get the terrain data
        TerrainData terrainData = proceduralTerain.terrain.terrainData;

        // Ensure the provided coordinates are within bounds
        startX = Mathf.Clamp(area[0], 0, terrainData.heightmapResolution - 1);
        endX = Mathf.Clamp(area[2], 0, terrainData.heightmapResolution - 1);
        startZ = Mathf.Clamp(area[1], 0, terrainData.heightmapResolution - 1);
        endZ = Mathf.Clamp(area[3], 0, terrainData.heightmapResolution - 1);

        // Set the height for the specified region
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        int startXEdge = (startX - 1 >= 0) ? startX - 1 : startX;
        int endXEdge = (endX + 1 <= terrainData.heightmapResolution - 1) ? endX + 1 : endX;
        int startZEdge = (startZ - 1 >= 0) ? startZ - 1 : startZ;
        int endZEdge = (endZ + 1 <= terrainData.heightmapResolution - 1) ? endZ + 1 : endZ;

        for(int i = startXEdge; i <= endXEdge; i++)
        {
            //get heights of neighbour point
            float terrainHeight = proceduralTerain.GetHeight(i,startZEdge);

            //set height to half of high difference
            heights[i, startZEdge] = targetHeight - (targetHeight - terrainHeight)/2;

            terrainHeight = proceduralTerain.GetHeight(i, endZEdge);

            heights[i, endZEdge] = targetHeight - (targetHeight - terrainHeight) / 2;
        }

        for (int i = startZEdge; i <= endZEdge; i++)
        {
            float terrainHeight = proceduralTerain.GetHeight(startXEdge, i);

            heights[startXEdge, i] = targetHeight - (targetHeight - terrainHeight) / 2;

            terrainHeight = proceduralTerain.GetHeight(endXEdge, i);

            heights[endXEdge, i] = targetHeight - (targetHeight - terrainHeight) / 2;
        }

        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                heights[x, z] = targetHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
}