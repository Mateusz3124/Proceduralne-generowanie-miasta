using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.VisualScripting;
using System.Linq;
using System.Security.Cryptography;
struct NoiseJob : IJobParallelFor
{
    public int row_length;
    public float scale;
    public float noise_scale;
    public float noise_height;
    public float offset;
    public float noise_offset;
    public NativeArray<Vector3> heights;
    public static FastNoiseLite noise = new FastNoiseLite();
    public void Execute(int i) {
        var xpos = (i % row_length) * scale + offset;
        var ypos = (i / row_length) * scale + offset;
        var noise_x = (xpos + noise_offset) * noise_scale;
        var noise_y = (ypos + noise_offset) * noise_scale;
        heights[i] = new Vector3(xpos, noise.GetNoise(noise_x, noise_y) * noise_height, ypos);
    }
};

public class ProceduralTerrain : MonoBehaviour
{
    public int batch_size = 8;
    public int size = 128;
    public int height = 5;
    public int resolution = 128;
    public float noise_scale = 1.0f;
    [HideInInspector] public float noise_offset = 0.0f;
    public Material terrain_material;
    public void Start() { }
    public void Generate()
    {
        noise_offset = UnityEngine.Random.Range(-1.0f, 1.0f) * resolution * size;
        var heights = new NativeArray<Vector3>(resolution * resolution, Allocator.TempJob);
        NoiseJob.noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        NoiseJob job = new NoiseJob{
            row_length = resolution,
            scale = (float)size / (float)(resolution - 1),
            noise_scale = noise_scale,
            noise_height = height,
            offset = -size * 0.5f,
            noise_offset = noise_offset,
            heights = heights
        };
        var handle = job.Schedule(heights.Length, batch_size);


        int[] inds = new int[(resolution-1)*(resolution-1)*6];
        uint idx = 0;
        for (int i = 0; i < resolution - 1; ++i) {
            for (int j = 0; j < resolution - 1; ++j) {
                int currentIndex = i * resolution + j;
                inds[idx + 0] = currentIndex + 1;
                inds[idx + 1] = currentIndex;
                inds[idx + 2] = currentIndex + resolution;
                inds[idx + 3] = currentIndex + 1;
                inds[idx + 4] = currentIndex + resolution;
                inds[idx + 5] = currentIndex + resolution + 1;
                idx += 6;
            }
        }

        handle.Complete();

        Vector2[] uvs = new Vector2[resolution * resolution];
        float inv_res = 1.0f / (float)resolution;
        for (int i = 0; i < heights.Length; ++i)
        {
            uvs[i] = new Vector2((i % resolution) * inv_res, i * (inv_res * inv_res));
        }

        Mesh mesh = new Mesh();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = terrain_material;
        gameObject.GetComponent<MeshRenderer>().material.SetFloat("", 1.0f);
        gameObject.AddComponent<MeshFilter>();
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = heights.ToArray();
        mesh.triangles = inds.ToArray();
        heights.Dispose();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    public float getHeight(float x, float y)
    {
        return NoiseJob.noise.GetNoise((x + noise_offset) * noise_scale, (y + noise_offset) * noise_scale) * height;
    }

    public float getHeight(Vector3 pos)
    {
        return getHeight(pos.x, pos.z);
    }

    public float2 GetMinCorner()
    {
        return new float2(-size * 0.5f);
    }

    public float2 GetMaxCorner()
    {
        return new float2(size * 0.5f);
    }
};