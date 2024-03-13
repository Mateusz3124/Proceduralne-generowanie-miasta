using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

public class river_mesh : MonoBehaviour
{
    public void CreateMesh(Spline s, float river_width)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int index_offset = vertices.ToArray().Length;

        //top verticies
        for (uint i = 0; i <= river_segments; ++i)
        {
            float t = (float)i / (float)river_segments;
            var pos = s.EvaluatePosition(t);
            var tan = s.EvaluateTangent(t);
            var bitan = Vector3.Normalize(Vector3.Cross(Vector3.up, tan)) * river_width * 0.5f;
            var p1 = pos - new float3(bitan);
            var p2 = pos + new float3(bitan);
            vertices.Add(p1);
            vertices.Add(p2);
        }
        //top indices
        for (int i = 0; i < 2 * (river_segments); i += 2)
        {
            indices.Add(index_offset + i);
            indices.Add(index_offset + i + 2);
            indices.Add(index_offset + i + 1);
            indices.Add(index_offset + i + 1);
            indices.Add(index_offset + i + 2);
            indices.Add(index_offset + i + 3);
        }
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    [SerializeField] uint river_segments = 10;
}
