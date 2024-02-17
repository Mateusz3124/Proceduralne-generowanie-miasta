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
    private Texture2D texture_regions;

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

        CreateRegion regions = new CreateRegion();
        regions.createRegions(proceduralTerrain, segmentList);
        const float num_regions = 5.0f;


        var step = 100;
        var width = proceduralTerrain.borderX / step;
        var height = proceduralTerrain.borderZ / step;
        texture_regions = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        for(var i=0; i<proceduralTerrain.borderX; i+=step) {
            for(var j=0; j<proceduralTerrain.borderZ; j+=step) {
                var a = (float)regions.getRegion(i, j) / num_regions;
                Color color = new Color(a, 0.0f, 0.0f, 1.0f);
                texture_regions.SetPixel(i/step, j/step, color);
            }
        }
        texture_regions.Apply(false, true);
        proceduralTerrain.terrainMaterial.SetTexture("_regions", texture_regions); 
        //river_Control river = GetComponent<river_Control>();
        //river._proceduralTerrain = proceduralTerrain;
        //river.riverToTerrain(river.createRiver());

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
