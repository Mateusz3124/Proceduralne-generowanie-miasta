using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.Generate();

        river_Control river = GetComponent<river_Control>();
        river.riverWidth = riverWidth;
        river._proceduralTerrain = proceduralTerrain;
        HashSet<int2> riverData = river.createRandomRiver();

        RoadGen roadGen = GetComponent<RoadGen>();
        RoadGen.minCorner = proceduralTerrain.GetMinCorner();
        RoadGen.maxCorner = proceduralTerrain.GetMaxCorner();
        roadGen.river = river;
        List<Segment> segmentList = roadGen.GenerateSegments(Vector2.zero);

        CreateRegion regions = new CreateRegion();
        regions.createRegions(proceduralTerrain, segmentList);

        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, segmentList);
        sm.CreateMesh(GetComponent<SplineContainer>(), global_transform);

        // BuildingGen buildingGen = new BuildingGen();
        // BuildingGen.minCorner = proceduralTerrain.GetMinCorner();
        // BuildingGen.maxCorner = proceduralTerrain.GetMaxCorner();
        // buildingGen.river = river;
        // buildingGen.makeBuildingsOnScene();
    }
    
}
