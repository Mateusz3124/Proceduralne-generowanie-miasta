using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using Unity.Mathematics;

public class spline : MonoBehaviour
{
    Spline[] m_Splines = { new Spline() };
    int counter;
    int number;
    private RoadGenerator rg;
    private SplineContainer splineContainer;
    int num_cells_in_row;
    public List<List<float3>> mpoint;
    bool[][] visited;
    // this is used to add extra space between terrain and road used for function tests or to make stuff look better 
    float extraHeight = 0f;

    // Point reduction epsilon determines how aggressive the point reduction algorithm is when removing redundant
    // points. Lower values result in more accurate spline representations of the original line, at the cost of
    // greater number knots.
    float m_PointReductionEpsilon = .15f;


    // Tension affects how "curvy" splines are at knots. 0 is a sharp corner, 1 is maximum curvitude.
    float m_SplineTension = 1f;
    void FindFirstPoint(out int x, out int y)
    {
        for (int i = 0; i < num_cells_in_row; ++i)
        {
            for (int j = 0; j < num_cells_in_row; ++j)
            {
                if(rg.road_type_grid[i, j] == RoadType.Road)
                {
                    if(CheckNumberOfNeighbours(i,j) > 2)
                    {
                        x = i;
                        y = j;
                        return;
                    }
                }
            }
        }
        throw new System.Exception("no 3-way or 4-way intersection");
    }

    void Start()
    {

    }
    // main function with create points
    public void CreatePoints()
    {
        rg = GameObject.Find("Terrain").GetComponent<RoadGenerator>();
        splineContainer = GameObject.Find("Terrain").GetComponent<SplineContainer>();
        num_cells_in_row = rg.num_cells_in_row;
        mpoint = new List<List<float3>>();
        visited = new bool[num_cells_in_row][];
        for (int i = 0;i < num_cells_in_row; i++) 
        {
            visited[i] = new bool[num_cells_in_row];
            for (int j = 0; j < num_cells_in_row; j++)
            {
                visited[i][j] = false;
            }
        }

        int x, y;
        FindFirstPoint(out x, out y);
        GoInDirections(x,y);

        CreateSplines(mpoint.Count);
        BuildSplineBasedOnPoints();
    }
    //Go into all directions from intersection or turn
    void GoInDirections(int x, int y)
    {
        var road_type_grid = rg.road_type_grid;
        var road_tile_size = rg.GetRoadTileSize();
        var theTerrain = rg.GetTerrain();
        //use only in loop
        int gridX = x - 1;
        int gridY = y;
        //left
        List<float3> listLeft = new List<float3>();
        if(gridX >= 0)
        {
            if (!visited[gridX][gridY])
            {
                while (road_type_grid[gridX, gridY] == RoadType.Road)
                {
                    visited[gridX][gridY] = true;
                    float realX = rg.plane_min_corner.x + road_tile_size * gridX, realY = rg.plane_min_corner.y + road_tile_size * gridY;
                    if (CheckNeighboursNotInDirection(gridX, gridY, Direction.Left) == 0)
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size;
                        start_pos[1] = realY + road_tile_size / 2;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listLeft.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                    }
                    else
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size;
                        start_pos[1] = realY + road_tile_size / 2;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listLeft.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                        GoInDirections(gridX, gridY);
                        break;
                    }
                    gridX = gridX - 1;
                }
            }
        }
        if (listLeft.Count > 0)
        {
            mpoint.Add(listLeft);
        }
        gridX = x + 1;
        gridY = y;
        //right
        List<float3> listRight = new List<float3>();
        if(gridX < num_cells_in_row)
        if (!visited[gridX][gridY])
        {
            while (road_type_grid[gridX, gridY] == RoadType.Road)
            {
                visited[gridX][gridY] = true;
                float realX = rg.plane_min_corner.x + road_tile_size * gridX, realY = rg.plane_min_corner.y + road_tile_size * gridY;
                if (CheckNeighboursNotInDirection(gridX, gridY, Direction.Right) == 0)
                {
                    float[] start_pos = new float[2];
                    start_pos[0] = realX;
                    start_pos[1] = realY + road_tile_size / 2;
                    Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                    float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                    listRight.Add(new float3(start_pos[0], start_height, start_pos[1]));
                }
                else
                {
                    float[] start_pos = new float[2];
                    start_pos[0] = realX;
                    start_pos[1] = realY + road_tile_size / 2;
                    Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                    float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                    listRight.Add(new float3(start_pos[0], start_height, start_pos[1]));
                    GoInDirections(gridX, gridY);
                    break;
                }
                gridX = gridX + 1;
            }
        }
        if (listRight.Count > 0)
        {
            mpoint.Add(listRight);
        }
        gridX = x;
        gridY = y + 1;
        //top
        List<float3> listTop = new List<float3>();
        if (gridY < num_cells_in_row)
        {
            if (!visited[gridX][gridY])
            {
                while (road_type_grid[gridX, gridY] == RoadType.Road)
                {
                    visited[gridX][gridY] = true;
                    float realX = rg.plane_min_corner.x + road_tile_size * gridX, realY = rg.plane_min_corner.y + road_tile_size * gridY;
                    if (CheckNeighboursNotInDirection(gridX, gridY, Direction.Top) == 0)
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size / 2;
                        start_pos[1] = realY;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listTop.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                    }
                    else
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size / 2;
                        start_pos[1] = realY;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listTop.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                        GoInDirections(gridX, gridY);
                        break;
                    }
                    gridY = gridY + 1;
                }
            }
        } 
        if (listTop.Count > 0)
        {
            mpoint.Add(listTop);
        }
        //down
        gridX = x;
        gridY = y - 1;
        List<float3> listDown = new List<float3>();
        if(gridY >= 0)
        {
            if (!visited[gridX][gridY])
            {
                while (road_type_grid[gridX, gridY] == RoadType.Road)
                {
                    visited[gridX][gridY] = true;
                    float realX = rg.plane_min_corner.x + road_tile_size * gridX, realY = rg.plane_min_corner.y + road_tile_size * gridY;
                    if (CheckNeighboursNotInDirection(gridX, gridY, Direction.Bottom) == 0)
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size / 2;
                        start_pos[1] = realY + road_tile_size;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listDown.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                    }
                    else
                    {
                        float[] start_pos = new float[2];
                        start_pos[0] = realX + road_tile_size / 2;
                        start_pos[1] = realY + road_tile_size;
                        Vector3 signPosition = new Vector3(start_pos[0], 0, start_pos[1]);
                        float start_height = theTerrain.terrain.SampleHeight(signPosition) + extraHeight;
                        listDown.Add(new Vector3(start_pos[0], start_height, start_pos[1]));
                        GoInDirections(gridX, gridY);
                        break;
                    }
                    gridY = gridY - 1;
                }
            }
        }
        if (listDown.Count > 0)
        {
            mpoint.Add(listDown);
        }
    }

    private enum Direction
    {
        Left, Right, Top, Bottom
    }

    int CheckNeighboursNotInDirection(int x, int y, Direction direction)
    {
        int counter = 0;
        var road_type_grid = rg.road_type_grid;
        if (direction == Direction.Left || direction == Direction.Right)
        {
            if (y > 0)
            {
                if (road_type_grid[x, y - 1] == RoadType.Road) counter++;
            }
            if (y < num_cells_in_row - 1)
            {
                if (road_type_grid[x, y + 1] == RoadType.Road) counter++;
            }
        }
        if(direction == Direction.Top || direction == Direction.Bottom)
        {
            if (x > 0)
            {
                if (road_type_grid[x - 1, y] == RoadType.Road) counter++;
            }
            if (x < num_cells_in_row - 1)
            {
                if (road_type_grid[x + 1, y] == RoadType.Road) counter++;
            }
        }



        return counter;
    }

    int CheckNumberOfNeighbours(int x, int y)
    {
        int counter = 0;
        var road_type_grid = rg.road_type_grid;
        if (y > 0)
        {
            if (road_type_grid[x, y - 1] == RoadType.Road) counter++;
        }
        if (y < num_cells_in_row - 1)
        { 
            if(road_type_grid[x, y + 1] == RoadType.Road) counter++;
        }
        if (x > 0)
        { 
            if(road_type_grid[x - 1, y] == RoadType.Road) counter++;
        }
        if (x < num_cells_in_row - 1)
        { 
            if(road_type_grid[x + 1, y] == RoadType.Road) counter++;
        }
        return counter;
    }


    void CreateSplines(int number)
    {
        // how many splines needed
        this.number = number;
        // Here initialize all necessary Splines in Holder
        // necessary to start set
        Holder = null;
        // here i give to SplineContainer Holder which must containts all Splines
        splineContainer.Splines = Holder;
    }

    void BuildSplineBasedOnPoints()
    {
        // shoulnd't be here probably forgot to delete
        //Flatten flat = new Flatten();
        //flat.rg = rg;
        //foreach (List<float3> list in mpoint)
        //{
        //    flat.flattenBasedOnPoint(list, 2);
        //}
        //int counter = 0;
        foreach (List<float3> list in mpoint)
        {
            //List<Vector3> m_Reduced = new List<Vector3>();
            // Before setting spline knots, reduce the number of sample points.
            //SplineUtility.ReducePoints(list, m_Reduced, m_PointReductionEpsilon);

            var spline = splineContainer[counter];

            // Assign the reduced sample positions to the Spline knots collection. Here we are constructing new
            // BezierKnots from a single position, disregarding tangent and rotation. The tangent and rotation will be
            // calculated automatically in the next step wherein the tangent mode is set to "Auto Smooth."
            spline.Knots = list.Select(x => new BezierKnot(x));
            var all = new SplineRange(0, spline.Count);

            // Sets the tangent mode for all knots in the spline to "Auto Smooth."
            spline.SetTangentMode(all, TangentMode.AutoSmooth);

            // Sets the tension parameter for all knots. Note that the "Tension" parameter is only applicable to
            // "Auto Smooth" mode knots.
            spline.SetAutoSmoothTension(all, m_SplineTension);
            counter ++;
        }
    }

    // this type of value (IReadOnlyList) is needed to create new spline
    public IReadOnlyList<Spline> Holder
    {
        get
        {
            return m_Splines;
        }
        set
        {
            // when i tried m_splines = value it gave error so i just initialize here
            m_Splines = new Spline[number];
            for (int i = 0;i < number; i++)
            {
                m_Splines[i] = new Spline();
            }
        }
    }
}
