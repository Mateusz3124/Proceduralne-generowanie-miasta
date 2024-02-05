using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Control : MonoBehaviour
{
    const float SEGMENT_COLLIDER_WIDTH = 10.0f;
    // Start is called before the first frame update
    private ProceduralTerrain proceduralTerrain;
    void Start()
    {
        proceduralTerrain = GetComponent<ProceduralTerrain>();
        proceduralTerrain.generate();
        RoadGen roadGen = GetComponent<RoadGen>();
        splineCreation splines = GetComponent<splineCreation>();
        splines.createSplines(proceduralTerrain, roadGen);
        SplineRoadMesh splineRoadMesh = GetComponent<SplineRoadMesh>();
        splineRoadMesh.createMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
