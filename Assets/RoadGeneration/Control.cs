using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class Control : MonoBehaviour
{
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    // Start is called before the first frame update
    private ProceduralTerrain proceduralTerrain;
    public SplineMesh sm;
    void Start()
    {
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.generate();

        RoadGen roadGen = GetComponent<RoadGen>();
        roadGen.minCorner = new Vector2(0f, 0f);
        roadGen.maxCorner = new Vector2(proceduralTerrain.borderX, proceduralTerrain.borderZ);
        
        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, roadGen);
        sm.CreateMesh(GetComponent<SplineContainer>(), transform);

        river_Control river = GetComponent<river_Control>();
        river._proceduralTerrain = proceduralTerrain;
        river.riverToTerrain(river.createRiver());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
