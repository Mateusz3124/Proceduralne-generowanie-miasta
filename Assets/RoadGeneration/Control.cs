using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class Control : MonoBehaviour
{
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


        var segmentList = roadGen.GenerateSegments(proceduralTerrain.center);

        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, segmentList);
        sm.CreateMesh(GetComponent<SplineContainer>(), transform);

        createRegion regions = new createRegion();
        regions.createRegions(proceduralTerrain, segmentList);

        //river_Control river = GetComponent<river_Control>();
        //river._proceduralTerrain = proceduralTerrain;
        //river.riverToTerrain(river.createRiver());

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
