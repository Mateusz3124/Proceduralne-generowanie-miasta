using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMesh : MonoBehaviour
{
	static List<GameObject> meshes = new List<GameObject>();

	[SerializeField] float road_width = 0.5f;
	[SerializeField] float road_height = 0.5f;
    [Tooltip("Multiples resolution based on road knot number (if road_segments_multipler = 2, and knot length = 5 -> road resolution = 10 :)")]
	[SerializeField] uint road_segments_multiplier = 1;
	[SerializeField] Material material;
	[SerializeField] ProceduralTerrain terrain;

	public GameObject AddGameObject() {
        meshes.Add(new GameObject());
        var go = meshes[meshes.Count() - 1];
        go.transform.SetParent(Control.global_transform);
        go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        return go;
    }

    public void findIntersections(List<Segment> segmentList)
    {
        foreach (Segment s in segmentList)
        {
            Vector3 startPos = new Vector3(s.start.x, 0, s.start.y);
            Vector3 endPos = new Vector3(s.end.x, 0, s.end.y);
            if(s.linksB.Count == 1) //laczy sie po prostu z droga
            {
				GameObject go = AddGameObject();
                go.name = "Zakret";
                go.transform.position = startPos;
			}
            if(s.linksF.Count == 1) //bedzie podwojnie wiec trzeba wykrywac ktore sa juz zrobione
            {
				GameObject go = AddGameObject();
                go.name = "Zakret";
                go.transform.position = endPos;
			}
            if(s.linksB.Count > 1) //bedzie podwojnie wiec trzeba wykrywac ktore sa juz zrobione
            {
				GameObject go = AddGameObject();
                go.name = "Skrzyzowanie forw z " + s.linksB.Count.ToString();
                go.transform.position = startPos;
			}
            if(s.linksF.Count > 1) //bedzie podwojnie wiec trzeba wykrywac ktore sa juz zrobione
            {
				GameObject go = AddGameObject();
                go.name = "Skrzyzowanie back z " + s.linksB.Count.ToString();
                go.transform.position = endPos;
			}

        }
	}

    public void CreateMesh(SplineContainer s, Transform parent)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
		List<Vector2> uvs = new List<Vector2>();

		foreach (var sp in s.Splines)
        {
			int index_offset = vertices.Count;
            uint road_segments = (uint)sp.Knots.Count() * road_segments_multiplier;

			//top verticies
			for (uint i = 0; i <= road_segments; ++i)
            {
                float t = (float)i / (float)road_segments;
                var pos = sp.EvaluatePosition(t);
                var tan = sp.EvaluateTangent(t);
                var bitan = Vector3.Normalize(Vector3.Cross(Vector3.up, tan)) * road_width * 0.5f;
                var p1 = pos - new float3(bitan);
                var p2 = pos + new float3(bitan);
                const float BIAS = 0.1f;
                var h0 = terrain.getHeight(pos) + BIAS;
                var h1 = Math.Max(h0, terrain.getHeight(p1)) + BIAS;
                var h2 = Math.Max(h0, terrain.getHeight(p2)) + BIAS;
                p1.y = h1;
                p2.y = h2;
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
                var p1 = pos - new float3(bitan);
                float p1TerrainPos = terrain.getHeight(p1.x, p1.z) - road_height;
                var p2 = pos + new float3(bitan);
                float p2TerrainPos = terrain.getHeight(p2.x, p2.z) - road_height;
                p1.y = p1TerrainPos;
                p2.y = p2TerrainPos;
                
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

			float uvOffset = 0;
            Debug.Log("OFF: " + index_offset);
            for (int i = 0; i <= road_segments * 4; i += 4)
			{
				float distance = Vector3.Distance(vertices[index_offset + i], vertices[index_offset + i + 3]) / 4.0f;
				float uvDistance = uvOffset + distance;

                uvs.AddRange(new List<Vector2> { new Vector2(uvOffset, 0), new Vector2(uvOffset, 1)
                    ,new Vector2(uvDistance, 0),new Vector2(uvDistance, 1)});

                uvOffset += uvDistance;
			}
		}

        Debug.Log(vertices.Count + " } " + uvs.Count);

		GameObject go = AddGameObject();
		go.transform.position = parent.position;
		Mesh mesh = go.GetComponent<MeshFilter>().mesh;

		mesh.vertices = vertices.ToArray();
		mesh.triangles = indices.ToArray();
        mesh.uv = uvs.ToArray();
	}
}
