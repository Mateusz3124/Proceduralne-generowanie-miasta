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
        go.AddComponent<MeshFilter>(); 
        go.AddComponent<MeshRenderer>(); 
        return go;
    }
    public void CreateMesh(SplineContainer s, Transform parent) {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        foreach(var sp in s.Splines) {
            sp.SetTangentMode(TangentMode.Broken);
            GameObject go = AddGameObject();
            go.transform.parent = parent;
            go.transform.position = parent.position;
            Mesh mesh = go.GetComponent<MeshFilter>().mesh;
            int index_offset = vertices.ToArray().Length;

            for(uint i=0; i<=road_segments; ++i) {
                var t = (float)i/(float)road_segments;
                var pos = sp.EvaluatePosition(t);
                var tan = sp.EvaluateTangent(t);
                var bitan = Vector3.Normalize(Vector3.Cross(Vector3.up, tan)) * road_width * 0.5f; 
                var p1 = pos - new float3(bitan);
                var p2 = pos + new float3(bitan);
                vertices.Add(p1);
                vertices.Add(p2);
            }

            for(int i=0; i<2 * (road_segments); i+=2) {
                indices.Add(index_offset + i); 
                indices.Add(index_offset + i+2); 
                indices.Add(index_offset + i+1); 
                indices.Add(index_offset + i+1); 
                indices.Add(index_offset + i+2); 
                indices.Add(index_offset + i+3); 
            }

            if(sp.Closed) {
                int last = indices[indices.Count() - 1];
                indices.Add(index_offset + last - 1);
                indices.Add(index_offset + 0);
                indices.Add(index_offset + last);
                indices.Add(index_offset + last);
                indices.Add(index_offset + 0);
                indices.Add(index_offset + 1);
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
        }
    } 

    [SerializeField] float road_width = 0.5f;
    [SerializeField] uint road_segments = 10;

    static List<GameObject> meshes = new List<GameObject>();
}
