using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildingGen
{
    const int BUILDING_SEGMENT_PERIOD = 3;
    const int BUILDINGS_PER_SEGMENT = 7;
    const float MAX_BUILDING_DISTANCE_FROM_SEGMENT = 30.0f;
    const float DEFAULT_BUILDING_HEIGHT = 70f;
    const float DEFAULT_BUILDING_WIDTH = 70f;

    // only for visualisation and testing
    // else dont use
    public void makeBuildingsOnScene() { 
        List<Building> buildings = GenerateBuildings();
        foreach(var b in buildings) {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(b.width, Random.Range(20f, 100f), b.height);
            cube.transform.rotation = Quaternion.Euler(0f, b.direction, 0f);
            cube.transform.position = new Vector3(b.center.x, 100f, b.center.y);
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
                float randomAngle = Random.Range(0.0f, 360.0f);
                float randomDistance = Random.Range(0.0f, MAX_BUILDING_DISTANCE_FROM_SEGMENT);
                float randomHeight = DEFAULT_BUILDING_HEIGHT + Random.Range(-20f, 20f);
                float randomWidth = DEFAULT_BUILDING_WIDTH + Random.Range(-20f, 20f);

                Vector2 center = (segment.start + segment.end) * 0.5f;
                center.x += randomDistance * Mathf.Sin(Mathf.Deg2Rad * randomAngle);
                center.y += randomDistance * Mathf.Cos(Mathf.Deg2Rad * randomAngle);
                Building building = new Building(center, randomHeight, randomWidth, segment.direction);

                int maxPlacementAttempts = 5;
                bool allowBuilding = false;
                for (int placementAttempt = 0; placementAttempt < maxPlacementAttempts; placementAttempt++)
                {
                    // check if building collides with any segment
                    List<Segment> matchesSeg = PhysicObjects.OverlapCircleSegments(building.center, building.circleColliderRadius);
                    if(matchesSeg.Count != 0) {
                        // move center away from collision
                        foreach(var s in matchesSeg) {
                            building.center += building.center - PhysicObjects.GetClosestPointOnSegment(
                                building.center, s.start, s.end);
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
                        allowBuilding = true;
                        break;
                    }
                }

                if(allowBuilding) {
                    building.MakeCollider();
                    buildings.Add(building);
                }
            }
        }

        return buildings;
    }
}
