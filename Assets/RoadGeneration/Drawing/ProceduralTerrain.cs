using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.VisualScripting;
using System.Linq;
struct NoiseJob : IJob {
    public float yoffset;
    public float row_length;
    public float scale;
    public float noise_scale;
    public float noise_height;
    public NativeArray<Vector3> heights;
    public void Execute() {
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        for(int i=0; i<heights.Length; ++i) {
            var xpos = (float)(i % row_length) * scale;
            var ypos = (float)(yoffset + (float)i/(float)row_length) * scale;
            heights[i] = new Vector3(xpos, noise.GetNoise(xpos*noise_scale, ypos*noise_scale) * noise_height, ypos);
        }
    }
};

public class ProceduralTerrain : MonoBehaviour { 
    public int num_jobs = 8;
    public int size = 128;
    public int height = 5;
    public int resolution = 128;
    public int noise_scale = 1;
    Mesh mesh;
    public void Start() { }
    public void Generate() {
        JobHandle[] handles = new JobHandle[num_jobs];
        NoiseJob[] jobs = new NoiseJob[num_jobs];
        int num_rows_for_job = (int)Math.Ceiling((float)resolution / (float)num_jobs);
        for(int i=0; i<num_jobs; ++i) {
            int yoffset = i * num_rows_for_job; 
            int num_rows_clamped = num_rows_for_job - Math.Max(0, yoffset+num_rows_for_job - resolution);
            int length = num_rows_clamped * resolution;
            NoiseJob job = new NoiseJob{
                yoffset = yoffset,
                row_length = resolution,
                scale = (float)size / (float)resolution,
                noise_scale = noise_scale,
                noise_height = height,
                heights = new NativeArray<Vector3>(resolution * num_rows_clamped, Allocator.TempJob),
            };
            jobs[i] = job;
            Debug.Log("JOB: yoff " + job.yoffset + " start: " + yoffset*resolution + " length: " + length);
            handles[i] = job.Schedule();
        }

        Vector3[] verts = new Vector3[resolution*resolution];
        Vector2[] uvs = new Vector2[resolution*resolution];
        for(int i=0; i<num_jobs; ++i) {
            handles[i].Complete();
            int yoffset = i * num_rows_for_job * resolution; 
            Debug.Log("COPYING DATA AT OFFSET " + yoffset);
            for(int j=0; j<jobs[i].heights.Length; ++j) {
                verts[yoffset + j] = jobs[i].heights[j];
                uvs[yoffset + j] = new Vector2(jobs[i].heights[j].x, jobs[i].heights[j].z);
                var v = verts[yoffset + j];
                Debug.Log("VERT: [" + v.x + " " + v.y + " " + v.z + "]");
            }
            jobs[i].heights.Dispose();
        }
        List<int> inds = new List<int>();
        for(int i=0; i<resolution-1; ++i) {
            for(int j=0; j<resolution-1; ++j) {
                inds.Add(i*resolution + j + 1);
                inds.Add(i*resolution + j);
                inds.Add(i*resolution + j + resolution);
                inds.Add(i*resolution + j + 1);
                inds.Add(i*resolution + j + resolution);
                inds.Add(i*resolution + j + resolution + 1);
            }
        }

        mesh = new Mesh();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>();
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        mesh.vertices = verts;
        mesh.triangles = inds.ToArray();
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
