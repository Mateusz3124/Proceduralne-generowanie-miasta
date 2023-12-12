using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

class VoronoiRegion
{
    public VoronoiRegion(GameObject region_square, Color region_color)
    {
        this.region_square = region_square;
        this.region_color = region_color;
    }
    public Color region_color;
    public GameObject region_square; // using center of tiles - fp numbers required
    public Dictionary<VoronoiRegion, List<Vector2Int>> neighbors = new Dictionary<VoronoiRegion, List<Vector2Int>>();
};


public class VoronoiRegionGenerator : MonoBehaviour
{
    [SerializeField] GameObject square;
    [SerializeField] Vector2Int map_size = new Vector2Int(100, 150);
    [SerializeField] int number_of_regions = 30;
    Vector3 start_square = new Vector3(0.5f, 0.0f, -0.5f);
    Vector3 row_dir = Vector3.back;
    Vector3 col_dir = Vector3.right;
    GameObject[,] instances;
    VoronoiRegion[,] instance_regions;
    List<VoronoiRegion> voronoi_regions = new List<VoronoiRegion>();

    void InitializeMap()
    {
        // init instances array
        instances = new GameObject[map_size.x, map_size.y];
        instance_regions = new VoronoiRegion[map_size.x, map_size.y];

        // display squares (tiles) on the screen; add instances to the map
        for (int row = 0; row < map_size.x; ++row)
        {
            for (int col = 0; col < map_size.y; ++col)
            {
                var pos = start_square + row * row_dir + col * col_dir;
                instances[row, col] = Instantiate(square, pos, Quaternion.identity);
            }
        }
    }

    void GenerateRandomVoronoiSeeds()
    {
        for (int i = 0; i < number_of_regions; ++i)
        {
            var ix = RandomNumberGenerator.GetInt32(0, map_size.x);
            var iy = RandomNumberGenerator.GetInt32(0, map_size.y);

            bool already_exists = false;
            foreach (var vor_reg in voronoi_regions)
            {
                if (vor_reg.region_square == instances[ix, iy])
                {
                    already_exists = true;
                    break;
                }
            }
            if (already_exists)
            {
                --i;
                continue;
            }

            VoronoiRegion vr = new VoronoiRegion(instances[ix, iy], new Color(
                (((float)RandomNumberGenerator.GetInt32(0, 51)) + 50.0f) / 100.0f,
                (((float)RandomNumberGenerator.GetInt32(0, 51)) + 50.0f) / 100.0f,
                (((float)RandomNumberGenerator.GetInt32(0, 51)) + 50.0f) / 100.0f));
            voronoi_regions.Add(vr);
        }
    }

    void GenerateVoronoiRegions()
    {
        for (int row = 0; row < map_size.x; ++row)
        {
            for (int col = 0; col < map_size.y; ++col)
            {
                var instance = instances[row, col];
                VoronoiRegion closest_region = null;
                float closest_distance = float.MaxValue;
                foreach (var vr in voronoi_regions)
                {
                    var distance = (instance.transform.position - vr.region_square.transform.position).magnitude;
                    if (distance >= closest_distance) { continue; }
                    closest_region = vr;
                    closest_distance = distance;
                }

                if (closest_region != null)
                {
                    instance_regions[row, col] = closest_region;
                    ChangeTileColor(instance, closest_region.region_color);
                }
            }
        }
    }

    Dictionary<GameObject, HashSet<VoronoiRegion>> GetNeighboringRegionsForEachTile()
    {
        Dictionary<GameObject, HashSet<VoronoiRegion>> instance_num_neighbors = new Dictionary<GameObject, HashSet<VoronoiRegion>>();
        for (int row = 0; row < map_size.x; ++row)
            for (int col = 0; col < map_size.y; ++col)
                instance_num_neighbors[instances[row, col]] = new HashSet<VoronoiRegion>();

        // create neighbor list
        for (int row = 0; row < map_size.x; ++row)
        {
            for (int col = 0; col < map_size.y; ++col)
            {
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i == 0 && j == 0) { continue; }

                        var nrow = Math.Clamp(row + i, 0, map_size.x - 1);
                        var ncol = Math.Clamp(col + j, 0, map_size.y - 1);

                        if (instance_regions[row, col] != instance_regions[nrow, ncol])
                        {
                            var a = instance_regions[row, col];
                            var b = instance_regions[nrow, ncol];
                            if (!a.neighbors.ContainsKey(b)) { a.neighbors.Add(b, new List<Vector2Int>()); }
                            if (!b.neighbors.ContainsKey(a)) { b.neighbors.Add(a, new List<Vector2Int>()); }
                            a.neighbors[b].Add(new Vector2Int(nrow, ncol));
                            b.neighbors[a].Add(new Vector2Int(row, col));
                            instance_num_neighbors[instances[row, col]].Add(b);
                        }
                    }
                }
            }
        }

        return instance_num_neighbors;
    }

    Dictionary<GameObject, HashSet<GameObject>> GetRoadNetwork()
    {
        var instance_num_neighbors = GetNeighboringRegionsForEachTile();
        List<HashSet<VoronoiRegion>> parsed_intersections = new List<HashSet<VoronoiRegion>>();
        Dictionary<GameObject, HashSet<VoronoiRegion>> go_adjacent_regs = new Dictionary<GameObject, HashSet<VoronoiRegion>>();
        for (int row = 0; row < map_size.x; ++row)
        {
            for (int col = 0; col < map_size.y; ++col)
            {
                var inst = instances[row, col];
                var ireg = instance_regions[row, col];
                var inn = instance_num_neighbors[inst];

                if (inn.ToArray().Length == 0) { continue; }
                if (row == 0 || row == map_size.x - 1 || (row != 0 && row != map_size.x && (col == 0 || col == map_size.y - 1)))
                {
                    if (parsed_intersections.Any(hs => hs.ToArray().Length == 2 && hs.Contains(ireg) && inn.All(e => hs.Contains(e))))
                    {
                        continue;
                    }
                }
                else if (inn.ToArray().Length < 2 || parsed_intersections.Any(nhs => nhs.Contains(ireg) && inn.All(e => nhs.Contains(e))))
                {
                    continue;
                }

                var hs = new HashSet<VoronoiRegion> { ireg };
                foreach (var vr in inn) { hs.Add(vr); }
                parsed_intersections.Add(hs);
                go_adjacent_regs.Add(inst, hs);
            }
        }

        Dictionary<GameObject, HashSet<GameObject>> intersections = new Dictionary<GameObject, HashSet<GameObject>>();
        foreach (var i in go_adjacent_regs)
        {
            foreach (var j in go_adjacent_regs)
            {
                if (i.Key == j.Key) { continue; }
                if (i.Value.Count(e => j.Value.Contains(e)) >= 2)
                {
                    if (!intersections.ContainsKey(i.Key)) { intersections[i.Key] = new HashSet<GameObject>(); }
                    if (!intersections.ContainsKey(j.Key)) { intersections[j.Key] = new HashSet<GameObject>(); }
                    intersections[i.Key].Add(j.Key);
                    intersections[j.Key].Add(i.Key);
                }
            }
        }

        return intersections;
    }

    void InvokeOnUniqueRoadConnection(Action<GameObject, GameObject> f)
    {
        TraverseRoadNetworkRec(GetRoadNetwork(), f);
    }

    void TraverseRoadNetworkRec(Dictionary<GameObject, HashSet<GameObject>> network, Action<GameObject, GameObject> custom_callback = null, GameObject node = null, Dictionary<GameObject, HashSet<GameObject>> drawn_connections = null)
    {
        if (drawn_connections == null) { drawn_connections = new Dictionary<GameObject, HashSet<GameObject>>(); }

        if (node == null)
        {
            foreach (var i in network)
            {
                TraverseRoadNetworkRec(network, custom_callback, i.Key, drawn_connections);
            }

            return;
        }
        if (drawn_connections.ContainsKey(node) && drawn_connections[node].ToArray().Length == network[node].ToArray().Length) { return; }

        if (!drawn_connections.ContainsKey(node)) { drawn_connections[node] = new HashSet<GameObject>(); }

        foreach (var i in network[node])
        {
            if (!drawn_connections.ContainsKey(i)) { drawn_connections[i] = new HashSet<GameObject>(); }
            if (drawn_connections[node].Contains(i) || drawn_connections[i].Contains(node)) { continue; }
            drawn_connections[node].Add(i);
            drawn_connections[i].Add(node);

            if (custom_callback != null) { custom_callback(node, i); }
            TraverseRoadNetworkRec(network, custom_callback, i, drawn_connections);
        }
    }

    void DebugDrawDebugLines()
    {
        InvokeOnUniqueRoadConnection((a, b) =>
        {
            Debug.DrawLine(a.transform.position, b.transform.position, Color.black, 10000.0f, false);
        });
    }

    void DebugPaintIntersectionTiles(Dictionary<GameObject, HashSet<GameObject>> network)
    {
        foreach (var i in network)
        {
            var l = i.Value.ToArray().Length;
            if(l == 0) { continue; }
            if(l <= 2) { ChangeTileColor(i.Key, Color.red); }
            if(l > 2) { ChangeTileColor(i.Key, Color.blue); }
        }
    }

    void ChangeTileColor(GameObject go, Color new_color)
    {
        go.GetComponent<MeshRenderer>().material.SetColor("_color", new_color);
    }

    void Start()
    {
        InitializeMap();
        GenerateRandomVoronoiSeeds();
        GenerateVoronoiRegions();
        var road_network = GetRoadNetwork();
        DebugPaintIntersectionTiles(road_network);
        DebugDrawDebugLines();
    }


}
