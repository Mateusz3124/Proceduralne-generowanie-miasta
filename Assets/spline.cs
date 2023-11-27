using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using Unity.Mathematics;

public class spline : MonoBehaviour
{
    int number = 0;
    List<float3> mpoint = new List<float3>(100);
    List<float3> m_Reduced = new List<float3>(20);
    Spline[] m_Splines = { new Spline() };

    // Point reduction epsilon determines how aggressive the point reduction algorithm is when removing redundant
    // points. Lower values result in more accurate spline representations of the original line, at the cost of
    // greater number knots.
    float m_PointReductionEpsilon = .15f;


    // Tension affects how "curvy" splines are at knots. 0 is a sharp corner, 1 is maximum curvitude.
    float m_SplineTension = 1f;



    void Start()
    {
        for (float i = 0; i < 100; i++)
        {
            mpoint.Add(new float3(i, 10.0f+i, 0.0f));
        }
        RebuildSpline();
    }

    void RebuildSpline()
    {
        // Before setting spline knots, reduce the number of sample points.
        SplineUtility.ReducePoints(mpoint, m_Reduced, m_PointReductionEpsilon);

        var xd = GetComponent<SplineContainer>();
        // give how many splines you need
        number = 5;
        // necessary to start set
        Holder = null;
        // here i give to SplineContainer Holder which must containts all Splines
        xd.Splines = Holder;
        // get Spline with index 1 
        var spline = xd[1];

        // Assign the reduced sample positions to the Spline knots collection. Here we are constructing new
        // BezierKnots from a single position, disregarding tangent and rotation. The tangent and rotation will be
        // calculated automatically in the next step wherein the tangent mode is set to "Auto Smooth."
        spline.Knots = m_Reduced.Select(x => new BezierKnot(x));

        var all = new SplineRange(0, spline.Count);

        // Sets the tangent mode for all knots in the spline to "Auto Smooth."
        spline.SetTangentMode(all, TangentMode.AutoSmooth);

        // Sets the tension parameter for all knots. Note that the "Tension" parameter is only applicable to
        // "Auto Smooth" mode knots.
        spline.SetAutoSmoothTension(all, m_SplineTension);
    }

    // this type of value (IReadOnlyList) is needed to create new spline
    public IReadOnlyList<Spline> Holder
    {
        get
        {
            return m_Splines;
        }
        set
        {
            // when i tried m_splines = value it gave error so i just initialize here
            m_Splines = new Spline[number];
            for (int i = 0;i < number; i++)
            {
                m_Splines[i] = new Spline();
            }
        }
    }
}
