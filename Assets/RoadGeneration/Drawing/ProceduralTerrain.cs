using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using UnityEditor;

public class ProceduralTerrain : MonoBehaviour
{
    public int width = 256;
    public int height = 256;

    public int depth = 17;

    public float scale = 7f;

    public float offsetX = 100f;
    public float offsetY = 100f;
    //must be here temporarly since some stuff use it 
    [HideInInspector]
    public Terrain terrain;
    //possible resolutions: 4097, 2049, 1025 , 513
    [Tooltip("possible resolutions: 4097, 2049, 1025 , 513")]
    public int resolution = 4097;

    public int numberOfTilesX = 2;
    public int numberOfTilesZ = 2;
    public Material terrainMaterial;

    private float[,] heightsMapLocal;
    [HideInInspector]
    public int borderX;
    [HideInInspector]
    public int borderZ;
    [HideInInspector]
    public Vector2 center;
    [HideInInspector]
    public GameObject[,] terrains;
    private void setHeights()
    {
        heightsMapLocal = new float[resolution * numberOfTilesZ, resolution * numberOfTilesX];
        for (int x = 0; x < resolution * numberOfTilesZ; x++)
        {
            for (int y = 0; y < resolution * numberOfTilesX; y++)
            {
                heightsMapLocal[x, y] = GetHeightPerlin(x, y);
            }
        }
    }
   public void generate()
    {
        offsetX = UnityEngine.Random.Range(0f, 9999f);
        offsetY = UnityEngine.Random.Range(0f, 9999f);
        setHeights();
        terrains = new GameObject[numberOfTilesX, numberOfTilesZ];
        borderX = width * numberOfTilesX;
        borderZ = height * numberOfTilesZ;
        for (int x= 0; x< numberOfTilesX; x++)
        {
            for(int z= 0; z < numberOfTilesZ; z++)
            {
                TerrainData terrainData = new TerrainData();
                terrainData = GenerateTerrain(terrainData, x* (resolution-1), z* (resolution-1));
                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainMaterial.SetFloat("_terrain_size", width);
                terrainObject.GetComponent<Terrain>().materialTemplate = terrainMaterial;
                terrainObject.transform.position = new Vector3Int(x*width, 0, z*height);
                terrains[x, z] = terrainObject;
            }
        }
        center.x = (float)numberOfTilesX/2*width;
        center.y = (float)numberOfTilesZ /2*height;
    }

    private void Start()
    {

    }

    private void Update()
    {

    }

    public float getHeight (float x, float z)
    {
        int tileX = (int)Math.Floor(x / width);
        int tileZ = (int)Math.Floor(z / height);
        GameObject temp;
        temp = terrains[tileX, tileZ];
        Terrain terrain = temp.GetComponent<Terrain>();
        Vector3 position = new Vector3(x, 0, z);
        float resultHeight = terrain.SampleHeight(position);
        return resultHeight;
    }

    TerrainData GenerateTerrain (TerrainData terrainData, int terrainOffSetY, int terrainOffSetX)
    {
        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(width, depth, height);

        float[,] heights = new float[resolution, resolution];
        for(int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                    heights[x, y] = heightsMapLocal[x+terrainOffSetX,y+terrainOffSetY];
            }
        }
        terrainData.SetHeights(0, 0, heights);
        return terrainData;
    }

    public float GetHeightPerlin (float x, float y)
    {
        float xCoord = x / width * scale + offsetX;
        float yCoord = y / height * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
