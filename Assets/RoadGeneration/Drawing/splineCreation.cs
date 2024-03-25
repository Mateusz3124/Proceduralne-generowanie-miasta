using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR;

public class splineCreation : MonoBehaviour
{
	public float minimumDistanceTreshold = 0.1f;
    public float heightOffset = 0.3f;

    public void createSplines(ProceduralTerrain proceduralTerrain, List<Segment> segmentList)
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        Dictionary<float3, List<Spline>> knotPositions = new Dictionary<float3, List<Spline>>();
        foreach (Segment s in segmentList)
        {
            createSpline(s, proceduralTerrain, splineContainer, knotPositions);
        }

		float3 knot1, knot2;
        BezierKnot newKnot;
		foreach (var xd in knotPositions)
        {
            if (xd.Value.Count == 1) //Po prostu sobie jest
            {
				continue;
            }
            else if(xd.Value.Count == 2) //zakret
            {
				//GameObject turn = GameObject.CreatePrimitive(PrimitiveType.Capsule);
				//turn.name = "Turn " + xd.Value.Count;
				//turn.transform.parent = Control.global_transform;
				//turn.transform.localScale = new Vector3(25, UnityEngine.Random.Range(20f, 100f), 25);
				//turn.transform.position = xd.Key;

                List<BezierKnot> sp1Knot = xd.Value[0].Knots.ToList();
                List<BezierKnot> sp2Knot = xd.Value[1].Knots.ToList();
				List<BezierKnot> points = new List<BezierKnot>();

				//Deleting duplicating knot
				if (sp1Knot.First().Position.Equals(xd.Key) || sp2Knot.Last().Position.Equals(xd.Key))
                {
                    knot1 = sp1Knot[1].Position;
                    knot2 = sp2Knot[sp2Knot.Count - 2].Position;
					newKnot = new BezierKnot((knot1 + knot2) / 2.0f);
                    sp1Knot.RemoveAt(0);
                    sp2Knot.RemoveAt(sp2Knot.Count - 1);

					points.AddRange(sp2Knot);
                    points.Add(newKnot);
					points.AddRange(sp1Knot);
				}
				else if (sp1Knot.Last().Position.Equals(xd.Key) || sp2Knot.First().Position.Equals(xd.Key))
				{
                    knot1 = sp1Knot[sp1Knot.Count - 2].Position;
					knot2 = sp2Knot[0].Position;
                    newKnot = new BezierKnot((knot1 + knot2) / 2.0f);
					sp1Knot.RemoveAt(sp1Knot.Count - 1);
					sp2Knot.RemoveAt(0);

					points.AddRange(sp1Knot);
					points.Add(newKnot);
					points.AddRange(sp2Knot);
				}
				else
                    Debug.Log("Lol powino znalezc duplikat knot'a");

				splineContainer.RemoveSpline(xd.Value[0]);
				splineContainer.RemoveSpline(xd.Value[1]);

                Spline newSpline = splineContainer.AddSpline();
                newSpline.Knots = points;
                newSpline.SetTangentMode(TangentMode.AutoSmooth);

                continue;
			}

			//Intersection
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "Intersection " + xd.Value;
			go.transform.parent = Control.global_transform;
			go.transform.localScale = new Vector3(25, UnityEngine.Random.Range(20f, 100f), 25);
			go.transform.position = xd.Key;
		}
    }
    public void createSpline(Segment segment, ProceduralTerrain proceduralTerrain, SplineContainer splineContainer, Dictionary<float3, List<Spline>> knotPositions)
    {
        Vector2 direction = segment.end - segment.start;
        float length = direction.magnitude;
        if(length < 1f)
        {
            return;
        }
        List<float3> list = new List<float3>();

        float3 positionFirst = new float3(segment.start.x, proceduralTerrain.getHeight(segment.start.x, segment.start.y) + heightOffset, segment.start.y);
        list.Add(positionFirst);
        //how far away are knots from each other
        float knotOffset = 20;

        if (length > knotOffset) //tutaj pamietaj treshold
        {
            float lengthFraction = knotOffset / length;
            int counter = 1;

            while(lengthFraction * counter< 1 - minimumDistanceTreshold)
            {
                Vector2 pointToAdd = segment.start + ((segment.end - segment.start) * lengthFraction * counter);
                counter++;
                float3 positionInside = new float3(pointToAdd.x, proceduralTerrain.getHeight(pointToAdd.x, pointToAdd.y) + heightOffset, pointToAdd.y);
                list.Add(positionInside);
            }
        }
        float3 positionLast = new float3(segment.end.x, proceduralTerrain.getHeight(segment.end.x, segment.end.y) + heightOffset, segment.end.y);
        list.Add(positionLast);

        Spline spline = splineContainer.AddSpline();
        spline.Knots = list.Select(x => new BezierKnot(x));
        spline.SetTangentMode(TangentMode.AutoSmooth);

        foreach (float3 knotPos in list)
        {
            if(!knotPositions.ContainsKey(knotPos))
            {
                List<Spline> sp = new List<Spline>();
                sp.Add(spline);
				knotPositions.Add(knotPos, sp);
            }
            else
                knotPositions[knotPos].Add(spline);
        }
    }
}