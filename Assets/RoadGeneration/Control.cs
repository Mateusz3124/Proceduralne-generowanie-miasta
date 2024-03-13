using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class Control : MonoBehaviour
{
    public GameObject transform_object;
    public static Transform global_transform;
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    private ProceduralTerrain proceduralTerrain;
    public SplineMesh sm;
    private Texture2D texture_regions;
    public int riverWidth;

    void Start()
    {
        global_transform = transform_object.transform;
        global_transform.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.parent = global_transform;
        Debug.Log("xd: " + Time.realtimeSinceStartup);
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.Generate();
        Debug.Log(Time.realtimeSinceStartup);

        RoadGen roadGen = GetComponent<RoadGen>();
        RoadGen.minCorner = proceduralTerrain.GetMinCorner();
        RoadGen.maxCorner = proceduralTerrain.GetMaxCorner();
        
        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, roadGen);
        sm.CreateMesh(GetComponent<SplineContainer>(), global_transform);

        BuildingGen buildingGen = new BuildingGen();
        BuildingGen.minCorner = proceduralTerrain.GetMinCorner();
        BuildingGen.maxCorner = proceduralTerrain.GetMaxCorner();
        buildingGen.makeBuildingsOnScene();
    }
    
}
