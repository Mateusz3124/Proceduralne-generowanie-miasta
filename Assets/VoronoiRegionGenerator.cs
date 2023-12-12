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

public class Pair<T1, T2>
{
    public Pair(T1 t1, T2 t2)
    {
        First = t1;
        Second = t2;
    }
    public T1 First { get; set; }
    public T2 Second { get; set; }
}

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

    void Start()
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

        // create voronoi regions
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

        // assign tiles to the regions and apply colors
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

        List<HashSet<VoronoiRegion>> intersections = new List<HashSet<VoronoiRegion>>();
        Dictionary<GameObject, HashSet<VoronoiRegion>> intersections_to_draw = new Dictionary<GameObject, HashSet<VoronoiRegion>>();
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
                    if (intersections.Any(hs => hs.ToArray().Length == 2 && hs.Contains(ireg) && inn.All(e => hs.Contains(e))))
                    {
                        continue;
                    }
                }
                else if (inn.ToArray().Length < 2 || intersections.Any(nhs => nhs.Contains(ireg) && inn.All(e => nhs.Contains(e))))
                {
                    continue;
                }


                var hs = new HashSet<VoronoiRegion> { ireg };
                foreach (var vr in inn) { hs.Add(vr); }
                intersections.Add(hs);
                intersections_to_draw.Add(inst, hs);
            }
        }

        foreach (var i in intersections_to_draw)
        {
            foreach (var j in intersections_to_draw)
            {
                if (i.Key == j.Key) { continue; }

                var bigger = i.Value.ToArray().Length >= j.Value.ToArray().Length ? i : j;
                var smaller = bigger.Key == i.Key ? j : i;

                if (i.Value.Count(e => j.Value.Contains(e)) >= 2)
                {
                    Debug.DrawLine(i.Key.transform.position, j.Key.transform.position, Color.black, 100000.0f, false);
                }
            }
        }

        foreach (var i in intersections_to_draw)
        {
            if (i.Value.ToArray().Length == 2)
            {
                ChangeTileColor(i.Key, Color.red);
            }
            else if (i.Value.ToArray().Length > 2)
            {
                ChangeTileColor(i.Key, Color.blue);
            }
        }
    }

    void ChangeTileColor(GameObject go, Color new_color)
    {
        go.GetComponent<MeshRenderer>().material.SetColor("_color", new_color);
    }
}
