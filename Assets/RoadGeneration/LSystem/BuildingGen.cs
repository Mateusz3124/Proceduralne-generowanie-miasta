using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGen
{
    const int BUILDING_SEGMENT_PERIOD = 5;
    const int BUILDINGS_PER_SEGMENT = 10;
    const float MAX_BUILDING_DISTANCE_FROM_SEGMENT = 400.0f;


    private List<Building> GenerateBuildings(List<Segment> segments)
    {
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

                Vector2 center = (segment.start + segment.end) * 0.5f;
                center.x += randomDistance * Mathf.Sin(Mathf.Deg2Rad * randomAngle);
                center.y += randomDistance * Mathf.Cos(Mathf.Deg2Rad * randomAngle);
                Building building = new Building(center, segment.direction);

                for (;false;)
                {
                    
                }

                building.MakeCollider();

                buildings.Add(building);
            }
        }

        return buildings;
    }
}
