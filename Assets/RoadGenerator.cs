using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;


public class RoadGenerator : MonoBehaviour
{

    [SerializeField] GameObject road_straight;
    [SerializeField] GameObject road_turn;
    [SerializeField] GameObject road_3way;
    [SerializeField] GameObject road_4way;
    [SerializeField] GameObject road_system_plane;
    // AABB of the plane
    Vector2 plane_min_corner, plane_max_corner;
    int num_cells_in_row;
    // The bigger the scale, the bigger the plane and more roads are generated
    [SerializeField] uint road_system_scale = 1;
    [SerializeField] int road_tile_size = 2;
    // % chance to generate junction in [0, 100]
    [SerializeField] int chance_to_pick_dir;

    enum RoadType
    {
        Empty, Obstacle, Road
    }
    // Square grid that holds information about the road system
    RoadType[,] road_type_grid;


    void Start()
    {
        road_system_plane.GetComponent<Transform>().localScale = new Vector3(road_system_scale, road_system_scale, road_system_scale);
        plane_min_corner.x = road_system_plane.GetComponent<MeshFilter>().mesh.bounds.min.x * road_system_scale;
        plane_min_corner.y = road_system_plane.GetComponent<MeshFilter>().mesh.bounds.min.z * road_system_scale;
        plane_max_corner.x = road_system_plane.GetComponent<MeshFilter>().mesh.bounds.max.x * road_system_scale;
        plane_max_corner.y = road_system_plane.GetComponent<MeshFilter>().mesh.bounds.max.z * road_system_scale;

        var plane_width = (int)(plane_max_corner.x - plane_min_corner.x);
        num_cells_in_row = plane_width / road_tile_size;
        road_type_grid = new RoadType[num_cells_in_row, num_cells_in_row];
        InitializeRoadTypeGrid();
        GenerateRoad(new Vector2Int(num_cells_in_row / 2, num_cells_in_row / 2), 5); // start from center and use 5 iterations
        DrawRoadBasedOnRoadTypeGrid(); // using data in road_type_grid, instantiate correct prefabs to visualize the road.
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
        if (road_type_grid[grid_pos.x, grid_pos.y] == RoadType.Road) { previousIsAlreadyExistingRoad = true;  return 1; }
        else { previousIsAlreadyExistingRoad = false; }
        switch (array_dimension)
        {
            case 0:
                bool up = false, down = false;
                if (grid_pos.y > 0) { down = road_type_grid[grid_pos.x, grid_pos.y - 1] == RoadType.Road; }
                if (grid_pos.y < num_cells_in_row - 1) { up = road_type_grid[grid_pos.x, grid_pos.y + 1] == RoadType.Road; }
                if(up || down)
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

    void DrawRoadBasedOnRoadTypeGrid()
    {
        for (int i = 0; i < num_cells_in_row; ++i)
        {
            for (int j = 0; j < num_cells_in_row; ++j)
            {
                switch (road_type_grid[i, j])
                {
                    case RoadType.Road:
                        Quaternion rotation;
                        var road = Instantiate(GetRoadVariantBasedOnNeighboringTiles(new Vector2Int(i, j), out rotation));
                        float pos_x = plane_min_corner.x + road_tile_size * i, pos_z = plane_min_corner.y + road_tile_size * j;
                        road.GetComponent<Transform>().position = new Vector3(pos_x + road_tile_size / 2, 0.1f, pos_z + road_tile_size / 2);
                        road.GetComponent<Transform>().rotation = rotation * road.GetComponent<Transform>().rotation;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    /*
        Picks matching road tile according to it's neighbors
        and also calculates neccessary orientation to fit them.
    */
    GameObject GetRoadVariantBasedOnNeighboringTiles(Vector2Int grid_pos, out Quaternion orientation)
    {
        bool up = false, down = false, left = false, right = false;
        if (grid_pos.y > 0) { down = road_type_grid[grid_pos.x, grid_pos.y - 1] == RoadType.Road; }
        if (grid_pos.y < num_cells_in_row - 1) { up = road_type_grid[grid_pos.x, grid_pos.y + 1] == RoadType.Road; }
        if (grid_pos.x > 0) { left = road_type_grid[grid_pos.x - 1, grid_pos.y] == RoadType.Road; }
        if (grid_pos.x < num_cells_in_row - 1) { right = road_type_grid[grid_pos.x + 1, grid_pos.y] == RoadType.Road; }

        int num_of_non_empty_neighbors = 0;
        if (up) { ++num_of_non_empty_neighbors; }
        if (down) { ++num_of_non_empty_neighbors; }
        if (left) { ++num_of_non_empty_neighbors; }
        if (right) { ++num_of_non_empty_neighbors; }

        orientation = Quaternion.identity;
        switch (num_of_non_empty_neighbors)
        {
            case 1:
                if (up || down) { orientation = Quaternion.identity; }
                else { orientation = Quaternion.AngleAxis(90.0f, Vector3.up); }
                return road_straight;
            case 2:
                if (up && down)
                {
                    orientation = Quaternion.identity;
                    return road_straight;
                }
                if (left && right)
                {
                    orientation = Quaternion.AngleAxis(90.0f, Vector3.up);
                    return road_straight;
                }

                if (up && left) { orientation = Quaternion.identity; }
                else if (up && right) { orientation = Quaternion.AngleAxis(90.0f, Vector3.up); }
                else if (right && down) { orientation = Quaternion.AngleAxis(180.0f, Vector3.up); }
                else if (down && left) { orientation = Quaternion.AngleAxis(270.0f, Vector3.up); }
                return road_turn;
            case 3:
                if (right && left && down) { orientation = Quaternion.AngleAxis(90.0f, Vector3.up); }
                else if (right && left && up) { orientation = Quaternion.AngleAxis(-90.0f, Vector3.up); }
                else if (up && down && right) { orientation = Quaternion.identity; }
                else if (up && down && left) { orientation = Quaternion.AngleAxis(180.0f, Vector3.up); }
                return road_3way;
            case 4:
                return road_4way;
            default:
                throw new Exception("Single road tile without any road neighbors is an error in the generating algorithm");
        }
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
