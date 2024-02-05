using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

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
    [SerializeField] SplineContainer splineContainer = new SplineContainer();
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

    bool IsOnTheEdge(GameObject o)
    {
        var pos = o.transform.position;
        return pos.x > 1 && pos.x < map_size.y - 1 && Math.Abs(pos.z) > 1 && Math.Abs(pos.z) < map_size.x - 1;
    }

    /*
        This function probably could be a lot simpler, but there is just too many cases to handle in one go.
    */
    Dictionary<GameObject, HashSet<VoronoiRegion>> GetNeighboringRegionsForEachTile()
    {
        // Building a dictionary of every tile and it's neighboring regions (including it's own)
        Dictionary<GameObject, HashSet<VoronoiRegion>> intersections = new Dictionary<GameObject, HashSet<VoronoiRegion>>();
        for (int row = 0; row < map_size.x; ++row)
        {
            for (int col = 0; col < map_size.y; ++col)
            {
                var inst = instances[row, col];
                var reg = instance_regions[row, col];
                intersections.Add(inst, new HashSet<VoronoiRegion>() { reg });
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        var nrow = Math.Clamp(row + i, 0, map_size.x - 1);
                        var ncol = Math.Clamp(col + j, 0, map_size.y - 1);
                        var ninst = instances[nrow, ncol];
                        var nreg = instance_regions[nrow, ncol];
                        intersections[inst].Add(nreg);
                    }
                }
            }
        }

        // Series of loops that delete redundant intersections (if there are too many, they are too close to each other and the road looks bad)
        HashSet<GameObject> to_delete = new HashSet<GameObject>();

        // This loop handles intersections only on the border of the map.
        // This is a special case, as I remove intersections when they intersect same set of regions
        // But, for example, when there are only 2 regions, there will be 2 intersections of the same regions, BUT
        // lying on different edges of the map and I don't want to remove them, even though they are considered to be duplicates.
        // The list of bools is a list of 4 bool, each, when true, specifying that intersection
        // exists on a particular edge of the map, and there shouldn't be any more on that edge.
        Dictionary<HashSet<VoronoiRegion>, List<bool>> region_inter_edge_existence = new Dictionary<HashSet<VoronoiRegion>, List<bool>>();
        foreach (var item in intersections)
        {
            if (item.Value.ToArray().Length < 2) { to_delete.Add(item.Key); }
            if (item.Value.ToArray().Length == 2)
            {
                if (IsOnTheEdge(item.Key)) { to_delete.Add(item.Key); }
                else
                {
                    var pos = item.Key.transform.position;
                    var idx = -1;
                    if (pos.x < 1) { idx = 0; }
                    else if (pos.x > map_size.y - 1) { idx = 1; }
                    else if (Math.Abs(pos.z) < 1) { idx = 2; }
                    else if (Math.Abs(pos.z) > map_size.x - 1) { idx = 3; }
                    Debug.Assert(idx != -1);
                    List<bool> matching_list = null;
                    foreach (var hs in region_inter_edge_existence)
                    { if (hs.Key.All(e => item.Value.Contains(e))) { matching_list = hs.Value; break; } }

                    if (matching_list == null)
                    {
                        List<bool> l = new List<bool>() { false, false, false, false };
                        l[idx] = true;
                        region_inter_edge_existence.Add(item.Value, l);
                    }
                    else if (matching_list != null && !matching_list[idx]) { matching_list[idx] = true; }
                    else { to_delete.Add(item.Key); }
                }
            }
        }
        foreach (var item in to_delete) { intersections.Remove(item); }
        to_delete.Clear();

        // This loop deletes intersections of 3 or more regions.
        // For example, when there are 3 intersecting regions, there will be
        // many tiles that will be intersections of those 3 regions
        // and this loops delete all but one
        foreach (var i in intersections)
        {
            if (i.Value.ToArray().Length < 3) { continue; }
            if (to_delete.Contains(i.Key)) { continue; }
            foreach (var j in intersections)
            {
                if (j.Key == i.Key) { continue; }
                if (to_delete.Contains(j.Key)) { continue; }

                if (i.Value.ToArray().Length > 2 && i.Value.ToArray().Length == j.Value.ToArray().Length)
                {
                    if (i.Value.All(e => j.Value.Contains(e))) { to_delete.Add(j.Key); break; }
                }
            }
        }

        // This loops detects if there is a bigger set j, containing smaller set i.
        // It removes clusters of intersections.
        foreach (var i in intersections)
        {
            if (i.Value.ToArray().Length < 3) { continue; }
            if (to_delete.Contains(i.Key)) { continue; }
            HashSet<VoronoiRegion> superset = null;
            foreach (var j in intersections)
            {
                if (j.Value.ToArray().Length < 4) { continue; }
                if (i.Key == j.Key) { continue; }
                if (to_delete.Contains(j.Key)) { continue; }
                if (j.Value.ToArray().Length > i.Value.ToArray().Length && i.Value.All(e => j.Value.Contains(e)))
                {
                    superset = j.Value;
                    break;
                }
            }
            if (superset != null) { to_delete.Add(i.Key); }
        }
        foreach (var item in to_delete) { intersections.Remove(item); }

        return intersections;
    }

    // Connect actual tiles with each other, based on the number of shared regions. 
    Dictionary<GameObject, HashSet<GameObject>> GetRoadNetwork()
    {
        var neighbors = GetNeighboringRegionsForEachTile();
        Dictionary<GameObject, HashSet<GameObject>> connections = new Dictionary<GameObject, HashSet<GameObject>>();

        foreach (var i in neighbors)
        {
            if (!connections.ContainsKey(i.Key)) { connections.Add(i.Key, new HashSet<GameObject>()); }
            foreach (var j in neighbors)
            {
                if (i.Key == j.Key) { continue; }
                if (!connections.ContainsKey(j.Key)) { connections.Add(j.Key, new HashSet<GameObject>()); }

                if (i.Value.Count(e => j.Value.Contains(e)) > 1)
                {
                    connections[i.Key].Add(j.Key);
                    connections[j.Key].Add(i.Key);
                }
            }
        }

        return connections;
    }

    // This function will run your custom callback for every connected tile and pass the connected tiles
    void InvokeOnUniqueRoadConnection(Action<GameObject, GameObject> f)
    {
        TraverseRoadNetworkRec(GetRoadNetwork(), f);
    }

    void TraverseRoadNetworkRec(Dictionary<GameObject, HashSet<GameObject>> network, Action<GameObject, GameObject> custom_callback = null, GameObject node = null, Dictionary<GameObject, HashSet<GameObject>> visited = null)
    {
        if (visited == null) { visited = new Dictionary<GameObject, HashSet<GameObject>>(); }

        if (node == null)
        {
            foreach (var i in network)
            {
                TraverseRoadNetworkRec(network, custom_callback, i.Key, visited);
            }

            return;
        }
        
        if (visited.ContainsKey(node) && visited[node].ToArray().Length == network[node].ToArray().Length) { return; }
        if (!visited.ContainsKey(node)) { visited[node] = new HashSet<GameObject>(); }

        foreach (var i in network[node])
        {
            if (!visited.ContainsKey(i)) { visited[i] = new HashSet<GameObject>(); }
            if (visited[node].Contains(i) || visited[i].Contains(node)) { continue; }
            visited[node].Add(i);
            visited[i].Add(node);

            if (custom_callback != null) { custom_callback(node, i); }
            TraverseRoadNetworkRec(network, custom_callback, i, visited);
        }
    }

    // The lines are only visible when the "game" is running and you're in the Scene view, not Game.
    void DebugDrawDebugLines()
    {
        InvokeOnUniqueRoadConnection((a, b) =>
        {
            // Debug.DrawLine(a.transform.position, b.transform.position, Color.black, 10000.0f, false);
            var road = new Spline();
            var ta = a.transform.position;
            var tb = b.transform.position;
            ta.y += 0.1f;
            tb.y += 0.1f;

            road.Add(new BezierKnot(ta));
            road.Add(new BezierKnot(tb));
            splineContainer.AddSpline(road);
        });
    }

    void DebugPaintIntersectionTiles(Dictionary<GameObject, HashSet<GameObject>> network)
    {
        foreach (var i in network)
        {
            var l = i.Value.ToArray().Length;
            if (l == 0) { continue; }
            if (l <= 2) { ChangeTileColor(i.Key, Color.red); }
            if (l > 2) { ChangeTileColor(i.Key, Color.blue); }
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

        /*
            Use InvokeOnUniqueRoadConnection to work with the road network. See how DebugDrawDebugLines works.
        */

        GetNeighboringRegionsForEachTile();
        var road_network = GetRoadNetwork();
        DebugPaintIntersectionTiles(road_network);
        DebugDrawDebugLines();
    }


}
