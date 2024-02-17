using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class Control : MonoBehaviour
{
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    private ProceduralTerrain proceduralTerrain;
    public SplineMesh sm;
    void Start()
    {
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.generate();

        RoadGen roadGen = GetComponent<RoadGen>();
        RoadGen.minCorner = new Vector2(0f, 0f);
        RoadGen.maxCorner = new Vector2(proceduralTerrain.borderX, proceduralTerrain.borderZ);
        
        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, roadGen);
        sm.CreateMesh(GetComponent<SplineContainer>(), transform);

        BuildingGen buildingGen = new BuildingGen();
        buildingGen.makeBuildingsOnScene();
    }

}
