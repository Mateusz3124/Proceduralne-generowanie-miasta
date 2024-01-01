using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class Procedural_Terrain : MonoBehaviour
{
    public int width = 256;
    public int height = 256;

    public int depth = 17;

    public float scale = 7f;

    public float offsetX = 100f;
    public float offsetY = 100f;
    public int amount_of_blocks_per_tile = 5;

    public int resolution;

    public Terrain terrain;

    private void Start()
    {
        offsetX = UnityEngine.Random.Range(0f, 9999f);
        offsetY = UnityEngine.Random.Range(0f, 9999f);
        terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    private void Update()
    {

    }

    int findResolution(int targetValue)
    {
        int[] array = {33,65,129,257,513,1025,2049,4097};
        int closestValue = array.AsEnumerable().OrderBy(x => Math.Abs(targetValue - x)).First();
        return closestValue;
    }

    TerrainData GenerateTerrain (TerrainData terrainData)
    {
        if( width != height)
        {
            throw new Exception("won't work for now if no square");
        }
        resolution = findResolution(width * amount_of_blocks_per_tile);
        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(width, depth, height);

        float[,] heights = new float[resolution, resolution];
        for(int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                heights[x, y] = GetHeight((float)x, (float)y);
            }
        }

        terrainData.SetHeights(0, 0, heights);
        return terrainData;
    }

    public float GetHeight (float x, float y)
    {
        float xCoord = x / (resolution-1) * scale + offsetX;
        float yCoord = y / (resolution-1) * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
