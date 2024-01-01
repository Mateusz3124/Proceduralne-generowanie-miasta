using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;


public enum RoadType
{
    Empty, Obstacle, Road
}

public class Road_Generator : MonoBehaviour
{

    [SerializeField] Procedural_Terrain theTerrain;
    public SplineContainer splineContainer_prefab;
    // AABB of the plane
    public Vector2 plane_min_corner, plane_max_corner;
    public int num_cells_in_row;
    [SerializeField] int road_tile_size = 2;
    // % chance to generate junction in [0, 100]
    [SerializeField] int chance_to_pick_dir;
    [SerializeField] int number_of_iterations;
    [SerializeField] int max_num_4way_intersections = 60;
    public Vector2Int riverStart, riverEnd;
    // Square grid that holds information about the road system
    public RoadType[,] road_type_grid;

    public int GetRoadTileSize()
    {
        return road_tile_size;
    }

    public Procedural_Terrain GetTerrain()
    {
        return theTerrain;
    }

    void Start()
    {
        plane_min_corner.x = 0;
        plane_min_corner.y = 0;
        plane_max_corner.x = theTerrain.width;
        plane_max_corner.y = theTerrain.height;

        var plane_width = (int)(plane_max_corner.x - plane_min_corner.x);
        num_cells_in_row = plane_width / road_tile_size;
        road_type_grid = new RoadType[num_cells_in_row, num_cells_in_row];
        InitializeRoadTypeGrid();
        River river = new River(this);
        river.MakeRiver();
        GenerateRoad(new Vector2Int(num_cells_in_row / 2, num_cells_in_row / 2), number_of_iterations); // start from center and use 5 iterations
        // here we started doing main functions
        RoadTools rt = new RoadTools(this);
        rt.ExtrudeDeadEndingRoads();
        rt.DeleteRandom4WayIntersectionsUntilUnderLimit(max_num_4way_intersections);
        spline spline = new spline();
        spline.CreatePoints();
        SplineRoadMesh splineRoadMesh = new SplineRoadMesh();
        splineRoadMesh.segments = 20;
        splineRoadMesh.roadWidth = (float)road_tile_size/2;
        splineRoadMesh.radius = 0.15f;
        float3 gobjPos = transform.position;
        splineRoadMesh.gobjPos = gobjPos;
        splineRoadMesh.generateRoad();
        Flatten flatten = new Flatten();
        flatten.changeTerrainWhenMesh();
    }

    /*
        Algorithm start at start_pos, and decides in what direction it should go (in the next recursive call), based
        on chance_to_pick_dir % chance. Then it randomizes the length of the road. At the end of the road, it starts again, 
        until iteration == max_iteration.

        start_pos - indices to road_type_grid
        max_iterations - maximum number of recursions when generating road
        iteration - iteration counter for recursive stop condition
    */
    void GenerateRoad(Vector2Int start_pos, int max_iterations, int iteration = 0)
    {
        if (iteration == max_iterations) { return; }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

        for (int i = 0; i < directions.Length; ++i)
        {
            Vector2Int dir = directions[i];
            bool should_go_in_picked_dir = RandomNumberGenerator.GetInt32(0, 100) > (100 - chance_to_pick_dir);
            if (!should_go_in_picked_dir) { continue; }

            int road_length = RandomNumberGenerator.GetInt32(2, num_cells_in_row);
            Vector2Int road_end_pos = GenerateRoadSegment(start_pos, dir, road_length);
            GenerateRoad(road_end_pos, max_iterations, iteration + 1);
        }
    }

    /*
        Utility function to create a straight line of road.
        It performs a bounds check.

        start_pos - position whence to start generating the road line
        direction - 2d vector specyfing the axis of the line
        length - the length of the segment
    */
    Vector2Int GenerateRoadSegment(Vector2Int start_pos, Vector2Int direction, int length)
    {
        // if direction has non-zero x, then we go horizontally, so we iterate by columns
        int array_dimension = direction.x != 0 ? 0 : 1;
        int stop_index = start_pos[array_dimension] + length * direction[array_dimension];
        // Clamp in range (0, grid size-1) to not to go out of grid bounds.
        stop_index = Math.Max(0, Math.Min(road_type_grid.GetLength(array_dimension) - 1, stop_index));
        Vector2Int current_pos = start_pos;
        // check if there are any neighbours in previous position and current position if yes value is 2
        int neighbours_in_current_and_previous = 0;
        bool previousIsAlreadyExistingRoad = false;
        while (current_pos[array_dimension] != stop_index)
        {
            neighbours_in_current_and_previous = checkIfAnyNeighboursNotIndirection(array_dimension, current_pos, neighbours_in_current_and_previous, out previousIsAlreadyExistingRoad);
            // Counts to 2 because some roades ends one tile before border (suprisingly often)  
            if (neighbours_in_current_and_previous == 2)
            {
                if (previousIsAlreadyExistingRoad) { break; }
                current_pos -= direction;
                road_type_grid[current_pos.x, current_pos.y] = RoadType.Empty;
                break;
            }
            road_type_grid[current_pos.x, current_pos.y] = RoadType.Road;
            current_pos += direction;
        }
        return current_pos;
    }
    // method it counts how many unwanted neighbours there are.
    int checkIfAnyNeighboursNotIndirection(int array_dimension, Vector2Int grid_pos, int neighbours_in_current_and_previous, out bool previousIsAlreadyExistingRoad)
    {
        //if encounter existing road change previousIsAlreadyExistingRoad to true so it is not deleted
        if (road_type_grid[grid_pos.x, grid_pos.y] == RoadType.Road) { previousIsAlreadyExistingRoad = true; return 1; }
        else { previousIsAlreadyExistingRoad = false; }
        switch (array_dimension)
        {
            case 0:
                bool up = false, down = false;
                if (grid_pos.y > 0) { down = road_type_grid[grid_pos.x, grid_pos.y - 1] == RoadType.Road; }
                if (grid_pos.y < num_cells_in_row - 1) { up = road_type_grid[grid_pos.x, grid_pos.y + 1] == RoadType.Road; }
                if (up || down)
                {
                    return ++neighbours_in_current_and_previous;
                }
                return 0;
            case 1:
                bool left = false, right = false;
                if (grid_pos.x > 0) { left = road_type_grid[grid_pos.x - 1, grid_pos.y] == RoadType.Road; }
                if (grid_pos.x < num_cells_in_row - 1) { right = road_type_grid[grid_pos.x + 1, grid_pos.y] == RoadType.Road; }
                if (left || right)
                {
                    return ++neighbours_in_current_and_previous;
                }
                return 0;
        }
        throw new Exception("Bad value of array_dimension");
    }

    void InitializeRoadTypeGrid(RoadType init_type = RoadType.Empty)
    {
        for (uint i = 0; i < road_type_grid.GetLength(0); ++i)
        {
            for (uint j = 0; j < road_type_grid.GetLength(1); ++j)
            {
                road_type_grid[i, j] = init_type;
            }
        }
    }
}
