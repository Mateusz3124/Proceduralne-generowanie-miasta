using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.VisualScripting;
using System.Linq;
struct NoiseJob : IJobParallelFor {
    public int row_length;
    public float scale;
    public float noise_scale;
    public float noise_height;
    public NativeArray<Vector3> heights;

    public void Execute(int i) {
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        var xpos = (i % row_length) * scale;
        var ypos = (i / row_length) * scale;
        heights[i] = new Vector3(xpos, noise.GetNoise(xpos * noise_scale, ypos * noise_scale) * noise_height, ypos);
    }
};

public class ProceduralTerrain : MonoBehaviour { 
    public int batch_size = 8;
    public int size = 128;
    public int height = 5;
    public int resolution = 128;
    public int noise_scale = 1;
    public Material terrain_material;
    public void Start() { }
    public void Generate() {
        var heights = new NativeArray<Vector3>(resolution * resolution, Allocator.TempJob);
        NoiseJob job = new NoiseJob{
            row_length = resolution,
            scale = (float)size / (float)(resolution-1),
            noise_scale = noise_scale,
            noise_height = height,
            heights = heights
        };
        var handle = job.Schedule(heights.Length, batch_size);
        handle.Complete();

        Vector2[] uvs = new Vector2[resolution*resolution];
        for(int i=0; i<heights.Length; ++i) {
            uvs[i] = new Vector2(heights[i].x, heights[i].z);
        }
        List<int> inds = new List<int>();
        for (int i = 0; i < resolution - 1; ++i) {
            for (int j = 0; j < resolution - 1; ++j) {
                int currentIndex = i * resolution + j;
                inds.Add(currentIndex + 1);
                inds.Add(currentIndex);
                inds.Add(currentIndex + resolution);
                inds.Add(currentIndex + 1);
                inds.Add(currentIndex + resolution);
                inds.Add(currentIndex + resolution + 1);
            }
        }

        Mesh mesh = new Mesh();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = terrain_material;
        gameObject.AddComponent<MeshFilter>();
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = heights.ToArray();
        mesh.triangles = inds.ToArray();
        heights.Dispose();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }
};
// public class ProceduralTerrain : MonoBehaviour
// {
//     public int width = 256;
//     public int height = 256;

//     public int depth = 17;

//     public float scale = 7f;

//     public float offsetX = 100f;
//     public float offsetY = 100f;
//     //must be here temporarly since some stuff use it 
//     [HideInInspector]
//     public Terrain terrain;
//     //possible resolutions: 4097, 2049, 1025 , 513
//     [Tooltip("possible resolutions: 4097, 2049, 1025 , 513")]
//     public int resolution = 4097;

//     public int numberOfTilesX = 2;
//     public int numberOfTilesZ = 2;

//     private float[,] heightsMapLocal;
//     [HideInInspector]

//     public float2 borderMin, borderMax;
//     private GameObject[,] terrains;
//     private void setHeights()
//     {
//         heightsMapLocal = new float[resolution * numberOfTilesZ, resolution * numberOfTilesX];
//         for (int x = 0; x < resolution * numberOfTilesZ; x++)
//         {
//             for (int y = 0; y < resolution * numberOfTilesX; y++)
//             {
//                 heightsMapLocal[x, y] = GetHeightPerlin(x, y);
//             }
//         }
//     }
//     public void generate()
//     {
//         offsetX = UnityEngine.Random.Range(0f, 9999f);
//         offsetY = UnityEngine.Random.Range(0f, 9999f);
//         setHeights();
//         terrains = new GameObject[numberOfTilesX, numberOfTilesZ];
//         borderMin = new float2(-width * numberOfTilesX * 0.5f, -height * numberOfTilesZ * 0.5f);
//         borderMax = new float2(width * numberOfTilesX * 0.5f, height * numberOfTilesZ * 0.5f);
//         for (int x = 0; x < numberOfTilesX; x++)
//         {
//             for (int z = 0; z < numberOfTilesZ; z++)
//             {
//                 TerrainData terrainData = new TerrainData();
//                 terrainData = GenerateTerrain(terrainData, x * (resolution - 1), z * (resolution - 1));
//                 GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
//                 terrainObject.transform.SetParent(Control.global_transform);
//                 terrainObject.transform.position = new Vector3Int((int)borderMin.x + x * width, 0, (int)borderMin.y + z * height);
//                 terrains[x, z] = terrainObject;
//             }
//         }
//     }

//     private void Start()
//     {

//     }

//     private void Update()
//     {

//     }

//     public float getHeight(float x, float z)
//     {
//         int tileX = (int)Math.Floor(x / width);
//         int tileZ = (int)Math.Floor(z / height);
//         GameObject temp;
//         try
//         {
//             temp = terrains[tileX, tileZ];
//         }
//         catch (Exception)
//         {
//             return 0f;
//         }
//         Terrain terrain = temp.GetComponent<Terrain>();
//         Vector3 position = new Vector3(x, 0, z);
//         float resultHeight = terrain.SampleHeight(position);
//         return resultHeight;
//     }

//     TerrainData GenerateTerrain(TerrainData terrainData, int terrainOffSetY, int terrainOffSetX)
//     {
//         terrainData.heightmapResolution = resolution;
//         terrainData.size = new Vector3(width, depth, height);

//         float[,] heights = new float[resolution, resolution];
//         for (int x = 0; x < resolution; x++)
//         {
//             for (int y = 0; y < resolution; y++)
//             {
//                 heights[x, y] = heightsMapLocal[x + terrainOffSetX, y + terrainOffSetY];
//             }
//         }
//         terrainData.SetHeights(0, 0, heights);
//         return terrainData;
//     }

//     public float GetHeightPerlin(float x, float y)
//     {
//         float xCoord = x / width * scale + offsetX;
//         float yCoord = y / height * scale + offsetY;

//         return Mathf.PerlinNoise(xCoord, yCoord);
//     }
// }
