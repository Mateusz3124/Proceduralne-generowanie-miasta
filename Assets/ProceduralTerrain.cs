using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    public int width = 256;
    public int height = 256;

    public int depth = 17;

    public float scale = 7f;

    public float offsetX = 100f;
    public float offsetY = 100f;

    public Terrain terrain;

    private void Start()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
        terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    private void Update()
    {

    }

    TerrainData GenerateTerrain (TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);

        float[,] heights = new float[width, height];
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = GetHeight((float)x, (float)y);
            }
        }

        terrainData.SetHeights(0, 0, heights);
        return terrainData;
    }

    public float GetHeight (float x, float y)
    {
        float xCoord = x / width * scale + offsetX;
        float yCoord = y / height * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
