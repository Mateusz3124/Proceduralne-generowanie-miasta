using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Splines;

public class Control : MonoBehaviour
{
    public GameObject transform_object;
    public static Transform global_transform;
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    private ProceduralTerrain proceduralTerrain;
    public SplineMesh sm;
    void Start()
    {
        global_transform = transform_object.transform;
        global_transform.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.parent = global_transform;
        
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.Generate();

        // RoadGen roadGen = GetComponent<RoadGen>();
        // RoadGen.minCorner = proceduralTerrain.borderMin;
        // RoadGen.maxCorner = proceduralTerrain.borderMax;
        
        // splineCreation splines = GetComponent<splineCreation>();
        // splines.createSplines(proceduralTerrain, roadGen);
        // sm.CreateMesh(GetComponent<SplineContainer>(), global_transform);

        // BuildingGen buildingGen = new BuildingGen();
        // BuildingGen.minCorner = proceduralTerrain.borderMin;
        // BuildingGen.maxCorner = proceduralTerrain.borderMax;
        // buildingGen.makeBuildingsOnScene();
    }
    
}
