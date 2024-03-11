using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMesh : MonoBehaviour
{
    private GameObject AddGameObject() {
        meshes.Add(new GameObject());
        var go = meshes[meshes.Count() - 1];
        go.transform.SetParent(Control.global_transform);
        go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        return go;
    }
    public void CreateMesh(SplineContainer s, Transform parent)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        foreach (var sp in s.Splines)
        {
            GameObject go = AddGameObject();
            go.transform.position = parent.position;
            Mesh mesh = go.GetComponent<MeshFilter>().mesh;
            int index_offset = vertices.ToArray().Length;

            //top verticies
            for (uint i = 0; i <= road_segments; ++i)
            {
                var t = (float)i / (float)road_segments;
                var pos = sp.EvaluatePosition(t);
                var tan = sp.EvaluateTangent(t);
                var bitan = Vector3.Normalize(Vector3.Cross(Vector3.up, tan)) * road_width * 0.5f;
                var p1 = pos - new float3(bitan);
                var p2 = pos + new float3(bitan);
                vertices.Add(p1);
                vertices.Add(p2);
            }
            //bottom veritcies
            for (uint i = 0; i <= road_segments; ++i)
            {
                var t = (float)i / (float)road_segments;
                var pos = sp.EvaluatePosition(t);
                var tan = sp.EvaluateTangent(t);
                var bitan = Vector3.Normalize(Vector3.Cross(Vector3.up, tan)) * road_width * 0.5f;
                var p1 = pos - new float3(bitan) - new float3(0, 0.5f, 0);
                var p2 = pos + new float3(bitan) - new float3(0, 0.5f, 0);
                vertices.Add(p1);
                vertices.Add(p2);
            }
            //top indices
            for (int i = 0; i < 2 * (road_segments); i += 2)
            {
                indices.Add(index_offset + i);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + i + 1);
                indices.Add(index_offset + i + 1);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + i + 3);
            }
            //bottom indices
            for (int i = (2 * (int)road_segments) + 2; i <= 4 * (road_segments); i += 2)
            {
                indices.Add(index_offset + i);
                indices.Add(index_offset + i + 1);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + i + 1);
                indices.Add(index_offset + i + 3);
                indices.Add(index_offset + i + 2);
            }
            //side left
            for (int i = 0; i < 2 * road_segments; i += 2)
            {
                indices.Add(index_offset + i);
                indices.Add(index_offset + 2 + i + 2 * (int)road_segments);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + 2 + i + 2 * (int)road_segments);
                indices.Add(index_offset + 4 + i + 2 * (int)road_segments);
            }
            //side right
            for (int i = 1; i < 2 * road_segments; i += 2)
            {
                indices.Add(index_offset + i);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + 2 + i + 2 * (int)road_segments);
                indices.Add(index_offset + i + 2);
                indices.Add(index_offset + 4 + i + 2 * (int)road_segments);
                indices.Add(index_offset + 2 + i + 2 * (int)road_segments);
            }
            //back
            indices.Add(index_offset);
            indices.Add(index_offset + 1);
            indices.Add(index_offset + 2 * ((int)road_segments + 1) + 1);
            indices.Add(index_offset);
            indices.Add(index_offset + 2 * ((int)road_segments + 1) + 1);
            indices.Add(index_offset + 2 * ((int)road_segments + 1));
            //front
            indices.Add(index_offset + 2 * (int)road_segments);
            indices.Add(index_offset + (4 * (int)road_segments) + 3);
            indices.Add(index_offset + (2 * (int)road_segments) + 1);
            indices.Add(index_offset + 2 * (int)road_segments);
            indices.Add(index_offset + (4 * (int)road_segments) + 2);
            indices.Add(index_offset + (4 * (int)road_segments) + 3);

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
        }
    }

    [SerializeField] float road_width = 0.5f;
    [SerializeField] float road_height = 0.5f;
    [SerializeField] uint road_segments = 10;
    [SerializeField] Material material;

    static List<GameObject> meshes = new List<GameObject>();
}
