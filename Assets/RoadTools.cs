using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class RoadTools : MonoBehaviour
{
    RoadGenerator rg;
    public RoadTools(RoadGenerator rg)
    {
        this.rg = rg;
    }

    int num_cells_in_row { get => rg.num_cells_in_row; }
    RoadType[,] road_type_grid { get => rg.road_type_grid; }

    /*
        Searches the entire road grid for any road tiles that
        have only one neighbor - they are dead ending roads.
        After finding one, it erases that tile, and moves
        to the one neighbor of that tile to check if
        it still has one neighbor. It repeats that until
        reaching a tile with more than one neighbor
        or a grid edge.

        max_its is a value specifying how many iterations of
        whole grid scan to perform. When reaching grid's
        border, a parallel road to that edge will have become
        a dead end road and will be removed in the consecutive iteration.
        This is a place to optimize the algorithm and should
        allow for removal max_its param.
    */
    public void EraseDeadEndingRoads(int max_its = 100)
    {
        for (int it = 0; it < max_its; ++it)
        {
            bool no_dead_ending_roads = true;
            for (int i = 0; i < num_cells_in_row; ++i)
            {
                for (int j = 0; j < num_cells_in_row; ++j)
                {
                    if (road_type_grid[i, j] != RoadType.Road) { continue; }
                    no_dead_ending_roads = false;

                    bool[] road_neighbors = new bool[4];
                    int num_neighboring_roads = CountRoadNeighbors(new Vector2Int(i, j), out road_neighbors);

                    if (num_neighboring_roads == 1)
                    {
                        // It's a dead-end
                        Vector2Int erase_pos = Vector2Int.left;
                        if (road_neighbors[1]) { erase_pos = Vector2Int.right; }
                        else if (road_neighbors[2]) { erase_pos = Vector2Int.down; }
                        else if (road_neighbors[3]) { erase_pos = Vector2Int.up; }

                        Vector2Int cpos = new Vector2Int(i, j);
                        do
                        {
                            road_type_grid[cpos.x, cpos.y] = RoadType.Empty;
                            cpos += erase_pos;
                            if (cpos.x < 0 || cpos.y < 0 || cpos.x > num_cells_in_row - 1 || cpos.y > num_cells_in_row - 1) { break; }
                            num_neighboring_roads = CountRoadNeighbors(cpos, out road_neighbors);
                        } while (num_neighboring_roads == 1);
                    }
                }
            }
            if (no_dead_ending_roads) { break; }
        }
    }

    /*
        Same as EraseDeadEndingRoads, except it extrudes
        the road instead of erasing it.
    */
    public void ExtrudeDeadEndingRoads(int max_its = 100)
    {
        for (int it = 0; it < max_its; ++it)
        {
            bool no_dead_ending_roads = true;
            for (int i = 0; i < num_cells_in_row; ++i)
            {
                for (int j = 0; j < num_cells_in_row; ++j)
                {
                    if (road_type_grid[i, j] != RoadType.Road) { continue; }
                    no_dead_ending_roads = false;

                    bool[] road_neighbors = new bool[4];
                    int num_neighboring_roads = CountRoadNeighbors(new Vector2Int(i, j), out road_neighbors);

                    if (num_neighboring_roads == 1)
                    {
                        // It's a dead-end
                        Vector2Int erase_pos = Vector2Int.left;
                        if (road_neighbors[1]) { erase_pos = Vector2Int.right; }
                        else if (road_neighbors[2]) { erase_pos = Vector2Int.down; }
                        else if (road_neighbors[3]) { erase_pos = Vector2Int.up; }

                        Vector2Int cpos = new Vector2Int(i, j);
                        do
                        {
                            road_type_grid[cpos.x, cpos.y] = RoadType.Road;
                            cpos -= erase_pos;
                            if (cpos.x < 0 || cpos.y < 0 || cpos.x > num_cells_in_row - 1 || cpos.y > num_cells_in_row - 1) { break; }
                            num_neighboring_roads = CountRoadNeighbors(cpos, out road_neighbors);
                        } while (num_neighboring_roads == 1);
                    }
                }
            }
            if (no_dead_ending_roads) { break; }
        }
    }

    /*
        Counts how many type-road neighbors a single tile at the 'pos'
        position has.
        It ouputs a neighbor array in the second parameter 'neighbors'
        in the format [left, right, top, bottom].
    */
    public int CountRoadNeighbors(Vector2Int pos, out bool[] neighbors)
    {
        int i = pos.x;
        int j = pos.y;
        bool left = j == 0 ? false : road_type_grid[i, j - 1] == RoadType.Road;
        bool right = j == num_cells_in_row - 1 ? false : road_type_grid[i, j + 1] == RoadType.Road;
        bool top = i == 0 ? false : road_type_grid[i - 1, j] == RoadType.Road;
        bool bottom = i == num_cells_in_row - 1 ? false : road_type_grid[i + 1, j] == RoadType.Road;
        int num_neighboring_roads = 0;
        if (left) { ++num_neighboring_roads; }
        if (right) { ++num_neighboring_roads; }
        if (top) { ++num_neighboring_roads; }
        if (bottom) { ++num_neighboring_roads; }

        neighbors = new bool[] { left, right, top, bottom };
        return num_neighboring_roads;
    }

    /*
        Gets a list of positions encoded as Vector2Int on the
        road_type_grid that are roads with 4 type-road neighbors.
    */
    public List<Vector2Int> Get4WayIntersections()
    {
        List<Vector2Int> four_way_inters = new List<Vector2Int>();

        for (int i = 0; i < num_cells_in_row; ++i)
        {
            for (int j = 0; j < num_cells_in_row; ++j)
            {
                if (road_type_grid[i, j] != RoadType.Road) { continue; }

                bool[] ns;
                int num_neighboring_roads = CountRoadNeighbors(new Vector2Int(i, j), out ns);
                if (num_neighboring_roads > 2)
                {
                    four_way_inters.Add(new Vector2Int(i, j));
                }
            }
        }

        return four_way_inters;
    }
    
    /*
        This functions gets a list of 4 way intersection tiles in the
        road_type_grid and randomly deletes them until there are no more
        than the specified limit. After removing them, it trims the dead
        ending roads that arised from the intersection dissolution.
    */
    public void DeleteRandom4WayIntersectionsUntilUnderLimit(int limit)
    {
        List<Vector2Int> four_way_inters = Get4WayIntersections();
        while (four_way_inters.Count > limit)
        {
            var rand_index = RandomNumberGenerator.GetInt32(0, four_way_inters.Count);
            var intpos = four_way_inters[rand_index];
            four_way_inters.RemoveAt(rand_index);
            road_type_grid[intpos.x, intpos.y] = RoadType.Empty;
        }
        EraseDeadEndingRoads();
    }
}
