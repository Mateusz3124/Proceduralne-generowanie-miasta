using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class river_Control : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private int height;
    private int width;
    private GameObject[,] terrains;
    [HideInInspector]
    private ProceduralTerrain proceduralTerrain;
    public ProceduralTerrain _proceduralTerrain
    {
        get { return proceduralTerrain; }
        set { 
            proceduralTerrain = value;
            height = value.height; 
            width = value.width;
            terrains = value.terrains;
        }
    }
    public GameObject river;
    public int riverWidth = 5;

    public int2[] createRiver()
    {
        int2[] values = new int2[21 * height];
        List<float3> list = new List<float3>();
        for (int i = 0; i < 20*height; i++)
        {
            values[i] = new int2(110, i);
            list.Add(new float3(110, proceduralTerrain.getHeight(110,i) - 3f, i));
        }
        SplineContainer splineContainer = river.GetComponent<SplineContainer>();

        Spline spline = splineContainer.AddSpline();
        spline.Knots = list.Select(x => new BezierKnot(x));
        spline.SetTangentMode(TangentMode.AutoSmooth);

        return values;
    }

    public void riverToTerrain(int2[] positions)
    {
        flatten_for_river flatten = GetComponent<flatten_for_river>();
        flatten.PerimeterRampDistance = riverWidth;
        List<int2> list = new List<int2>();

        int tileX = positions[0].x / width;
        int tileZ = positions[0].y / height;
        list.Add(positions[0]);

        for (int i = 1; i < positions.Length; i++)
        {
            int presentX = positions[i].x / width;
            int presentZ = positions[i].y / height;

            if (tileX == presentX && tileZ == presentZ)
            {
                int2 chosenPoint = new int2(positions[i].x - presentX*width, positions[i].y - presentZ*height);
                list.Add(chosenPoint);
            }
            else
            {
                flatten.changeTerrain(terrains[tileX, tileZ], list);
                tileX = presentX;
                tileZ = presentZ;
            }
        }
    }
}
