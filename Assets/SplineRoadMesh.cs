using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineRoadMesh : MonoBehaviour
{
    [SerializeField]
    MeshFilter meshFilter;
    List<Vector3> rightVerticies;
    List<Vector3> leftVerticies;

    [SerializeField]
    SplineContainer roadSpline;

    [Range(3,100)]
    public int segments = 1;

    [Range(0.1f, 5)]
    public float roadWidth = 1f;

    [Range(0.1f,10f)]
    public float radius = 1f;

    public bool drawGizmos = false;

    float3 position;
    float3 forward;
    float3 upVector;

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        roadSpline = GetComponent<SplineContainer>();
    }

    private void OnValidate()
    {
        calculateRoadVerticies();
    }

    private void OnEnable()
    {
        Spline.Changed += splineChanged;
        calculateRoadVerticies();
    }

    private void OnDisable()
    {
        Spline.Changed -= splineChanged;
    }

    private void splineChanged(Spline arg1, int arg2, SplineModification arg3)
    {
        calculateRoadVerticies();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        calculateRoadVerticies(true);
    }

    private void calculateRoadVerticies(bool onGizmos = false)
    {
        int splineCount = roadSpline.Splines.Count;
        rightVerticies = new List<Vector3>();
        leftVerticies = new List<Vector3>();

        float3 gobjPos = transform.position;

        for (int splineIndex = 0; splineIndex < splineCount; splineIndex++)
        {
            bool isClosed = roadSpline.Splines[splineIndex].Closed;
            float step = (isClosed ? 1.0f / segments : 1.0f / (segments - 1));
            float currentPos = (isClosed ? step : 0);

            float3 rightDir, rightSide, leftSide;

            for (int i = 0; i < segments; i++)
            {
                roadSpline.Evaluate(splineIndex, currentPos, out position, out forward, out upVector);
                position -= gobjPos;
                
                rightDir = Vector3.Cross(forward, upVector).normalized;
                rightSide = position + rightDir * roadWidth;
                leftSide = position - rightDir * roadWidth;

                if (onGizmos)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(position, radius);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(rightSide, radius);
                    Gizmos.DrawSphere(leftSide, radius);
                    Gizmos.DrawLine(rightSide, leftSide);
                }

                rightVerticies.Add(rightSide);
                leftVerticies.Add(leftSide);
                currentPos += step;
            }

            roadSpline.Evaluate(splineIndex, 1f, out position, out forward, out upVector);
            position -= gobjPos;

            rightDir = Vector3.Cross(forward, upVector).normalized;
            rightSide = position + rightDir * roadWidth;
            leftSide = position - rightDir * roadWidth;

            rightVerticies.Add(rightSide);
            leftVerticies.Add(leftSide);
        }

        buildMesh();
    }

    private void buildMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verticies = new List<Vector3>();
        List<int> triangles = new List<int>();
        int offset;

        for (int currSplIdx = 0; currSplIdx < roadSpline.Splines.Count; currSplIdx++)
        {
            bool isClosed = roadSpline.Splines[currSplIdx].Closed;
            int splineOffset = segments * currSplIdx;
            splineOffset += currSplIdx;

            for (int i = 1; i <= segments; i++)
            {
                int vertOff = splineOffset + i;
                Vector3 p1 = rightVerticies[vertOff - 1];
                Vector3 p2 = leftVerticies[vertOff - 1];
                Vector3 p3, p4;

                if (isClosed && vertOff == splineOffset + segments)
                {
                    p3 = rightVerticies[splineOffset];
                    p4 = leftVerticies[splineOffset];
                }
                else
                {
                    p3 = rightVerticies[vertOff];
                    p4 = leftVerticies[vertOff];
                }

                offset = 4 * segments * currSplIdx;
                offset += 4 * (i - 1);

                int t1 = offset;
                int t2 = offset + 2;
                int t3 = offset + 3;

                int t4 = offset + 3;
                int t5 = offset + 1;
                int t6 = offset;

                verticies.AddRange(new[] { p1, p2, p3, p4 });
                triangles.AddRange(new[] { t1, t2, t3, t4, t5, t6 });
            }
        }

        mesh.SetVertices(verticies);
        mesh.SetTriangles(triangles, 0);
        meshFilter.mesh = mesh;
    }
}
