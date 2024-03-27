using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

public class BuildingGen
{
    const int BUILDING_SEGMENT_PERIOD = 2;
    const int BUILDINGS_PER_SEGMENT = 5;
    const float MAX_BUILDING_DISTANCE_FROM_SEGMENT = 200.0f;
    const float DISTANCE_TO_MOVE_IN_PLACING_ATTEMPT = 30f;
    const float DISTANCE_TO_MOVE_WHEN_RIVER = 30f;
    const float MAX_PLACEMENT_ATTEMPTS = 20;
    const float DEFAULT_BUILDING_HEIGHT = 70f;
    const float DEFAULT_BUILDING_WIDTH = 70f;

    public static Vector2 minCorner {get; set;} = new Vector2(0f, 0f);
    public static Vector2 maxCorner {get; set;} = new Vector2(512f, 512f);

    [HideInInspector]
    public river_Control river;
    private CreateRegion regions;

    public BuildingGen(CreateRegion regions) {
        this.regions = regions;
    }

    // only for visualisation and testing
    // else dont use
    public void makeBuildingsOnScene() { 
        List<Building> buildings = GenerateBuildings();
        foreach(var b in buildings) {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = Control.global_transform;
            cube.transform.localScale = new Vector3(b.width, UnityEngine.Random.Range(20f, 100f), b.height);
            cube.transform.rotation = Quaternion.Euler(0f, b.direction, 0f);
            cube.transform.position = new Vector3(b.center.x, 100f, b.center.y);

            if(PhysicObjects.pois.Contains(b)) {
                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = Color.red;
            }
        }
    }

    public List<Building> GenerateBuildings()
    {
        List<Segment> segments = PhysicObjects.segmentsColliders.ToList();
        List<Building> buildings = new List<Building>();

        // which segment will have buildings
        for (int i = 0; i < segments.Count; i += BUILDING_SEGMENT_PERIOD)
        {
            Segment segment = segments[i];

            // make BUILDINGS_PER_SEGMENT buildings
            for (int j = 0; j < BUILDINGS_PER_SEGMENT; j++)
            {
                float randomAngle = UnityEngine.Random.Range(0.0f, 360.0f);
                float randomDistance = UnityEngine.Random.Range(0.0f, MAX_BUILDING_DISTANCE_FROM_SEGMENT);
                float randomHeight = DEFAULT_BUILDING_HEIGHT + UnityEngine.Random.Range(-20f, 20f);
                float randomWidth = DEFAULT_BUILDING_WIDTH + UnityEngine.Random.Range(-20f, 20f);

                Vector2 center = (segment.start + segment.end) * 0.5f;
                center.x += randomDistance * Mathf.Sin(Mathf.Deg2Rad * randomAngle);
                center.y += randomDistance * Mathf.Cos(Mathf.Deg2Rad * randomAngle);
                Building building = new Building(center, randomHeight, randomWidth, segment.direction);

                bool allowBuilding = false;
                for (int placementAttempt = 0; placementAttempt < MAX_PLACEMENT_ATTEMPTS; placementAttempt++)
                {
                    // check if building collides with any segment
                    List<Segment> matchesSeg = PhysicObjects.OverlapCircleSegments(building.center, building.circleColliderRadius);
                    if(matchesSeg.Count != 0) {
                        // move center away from collision
                        foreach(var s in matchesSeg) {
                            building.center += (building.center - PhysicObjects.GetClosestPointOnSegment(
                                building.center, s.start, s.end)).normalized * DISTANCE_TO_MOVE_IN_PLACING_ATTEMPT;
                        }
                        continue;
                    }

                    // check if building collides with any building
                    List<Building> matchesBuild = PhysicObjects.OverlapCircleBuildings(building.center, building.circleColliderRadius);
                    if(matchesBuild.Count != 0) {
                        // move center away from collision
                        foreach(var b in matchesBuild) {
                            building.center += building.center - b.center;
                        }
                    }
                    else {
                        float distance;
                        float3 point;
                        bool ifInsideSquares;
                        (distance, point, ifInsideSquares) = river.ifRiver(building.center.x, building.center.y);
                        if (!ifInsideSquares)
                        {
                            allowBuilding = true;
                            break;
                        }
                        if(distance < 1.6* river.riverWidth)
                        {
                            break;
                        }
                        allowBuilding = true;
                        break;
                    }
                }
                
                if(allowBuilding && BuildingWithinLimits(building)) {
                    building.MakeCollider();
                    buildings.Add(building);
                }
            }
        }

        GeneratePois(buildings);

        return buildings;
    }

    private void GeneratePois(List<Building> buildings) {
        int buildingsCount = buildings.Count;
        int number_of_pois = (int)(buildingsCount * 0.03f);

        int nOfgeneratedPOIs = 0;
        while(nOfgeneratedPOIs != number_of_pois) {
            int randIndex = UnityEngine.Random.Range(0, buildingsCount);
            var building = buildings[randIndex];
            if(PhysicObjects.pois.Contains(building))
                continue;

            building.type = GetPoiType(building);
            PhysicObjects.pois.Add(building);
            nOfgeneratedPOIs++;
        }

        foreach(var b in PhysicObjects.pois) {
            Debug.Log(b.type);
        }
    }

    private BuildingType GetPoiType(Building building) {
        var regionType = regions.getRegion(building.center.x, building.center.y);
        var randValue = UnityEngine.Random.Range(0, 4);
        switch(regionType) {
            case CreateRegion.RegionType.skyscrapers:
                if(randValue <= 1)
                    return BuildingType.businessHub;
                else
                    return BuildingType.skyscraper;

            case CreateRegion.RegionType.highBuildings:
                if(randValue == 0)
                    return BuildingType.church;
                else if(randValue == 1)
                    return BuildingType.businessHub;
                else if(randValue == 2)
                    return BuildingType.gallery;
                else
                    return BuildingType.university;
            
            case CreateRegion.RegionType.lowBuildings:
                if(randValue == 0)
                    return BuildingType.university;
                else if(randValue == 1)
                    return BuildingType.cinema;
                else if(randValue == 2)
                    return BuildingType.gallery;
                else
                    return BuildingType.restaurant;

            case CreateRegion.RegionType.outskirts:
                if(randValue <= 1)
                    return BuildingType.sportsField;
                else
                    return BuildingType.park;

            case CreateRegion.RegionType.noroads:
                if(randValue <= 1)
                    return BuildingType.sportsField;
                else
                    return BuildingType.park;
        }

        return BuildingType.normal;
    }


    private bool BuildingWithinLimits(Building building) {
        if(building.center.x > minCorner.x && building.center.x < maxCorner.x &&
            building.center.y > minCorner.y && building.center.y < maxCorner.y) {
            return true;
        }

        return false;        
    }
}
