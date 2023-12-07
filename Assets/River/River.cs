using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class River
{
    private SplineContainer splineContainer_prefab;
    // riverStart's and riverEnd's x and y coords are position on road_type_grid not on scene
    // later it can be random
    private Vector2Int riverStart, riverEnd;
    private float riverY_coord = 0.1f;
    private SplineContainer river_splineContainer;
    private Spline river;
    private Vector2Int gridSize;
    private int tileSize;
    private RoadGenerator rg;

    public River(RoadGenerator rg) {
        this.rg = rg;
        gridSize = new Vector2Int(rg.num_cells_in_row, rg.num_cells_in_row);
        tileSize = rg.GetRoadTileSize();

        if(rg.riverStart.x < 0 || rg.riverStart.y >= rg.num_cells_in_row 
            || rg.riverEnd.x < 0 || rg.riverEnd.y >= rg.num_cells_in_row) {
            throw new System.Exception("Bad start or end point for the river");
        }
        riverStart = rg.riverStart;
        riverEnd = rg.riverEnd;
        
        splineContainer_prefab = rg.splineContainer_prefab;
        river_splineContainer = GameObject.Instantiate(splineContainer_prefab, rg.transform);
        river = river_splineContainer.AddSpline();
    }

    public void MakeRiver() {
        List<float3> riverKnots = AStarPathFind();

        river.Knots = riverKnots.Select(x => new BezierKnot(x));
        
        var all = new SplineRange(0, river.Count);
        float autoSmoothTension = 1f;
        river.SetTangentMode(all, TangentMode.AutoSmooth);
        river.SetAutoSmoothTension(all, autoSmoothTension);
    }

    // find path from riverStart to riverEnd
    List<float3> AStarPathFind() {
        // neighboursGrid is array to keep track of node connections, later to get path
        // and it has 3 dim vectors no 2 dim, because its simpler to calculate path
        Vector2Int[,] neighboursGrid = new Vector2Int[gridSize.x, gridSize.y];
        PriorityQueue<Vector2Int, float> nodesToVisit = new PriorityQueue<Vector2Int, float>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        nodesToVisit.Push(riverStart, 0);
        int shortestPath = int.MaxValue;

        while(nodesToVisit.length > 0) {
            Vector2Int currentNode = nodesToVisit.Pop();
            visited.Add(currentNode);
            if(currentNode.Equals(riverEnd)) {
                break;
            }
            
            foreach(var neighbour in GetNeighbours(currentNode, visited)) {
                if(!nodesToVisit.Contains(neighbour)) {
                    float distanceLinear = (riverEnd - neighbour).magnitude;
                    nodesToVisit.Push(neighbour, distanceLinear);
                    neighboursGrid[neighbour.x, neighbour.y] = currentNode - neighbour;
                }
                // this part is to fix
                // now can be erazed
                int pathLengthToNeighbour = ComputePathLenghtFrom(neighbour, neighboursGrid, currentNode - neighbour);
                if(pathLengthToNeighbour < shortestPath && nodesToVisit.Contains(neighbour)) {
                    shortestPath = pathLengthToNeighbour;
                    neighboursGrid[neighbour.x, neighbour.y] = currentNode - neighbour;
                } 
            }
        }
    
        return GetKnotsFromNeighboursGrid(neighboursGrid);
    }

    float3 ConvertToFloat3(Vector2Int vec) {
        return new float3(vec.x, riverY_coord, vec.y);
    }

    List<Vector2Int> GetNeighbours(Vector2Int pos, HashSet<Vector2Int> visited) {
        Vector2Int[] potentialNeighbours = {
            //left
            pos + Vector2Int.left,
            //right
            pos + Vector2Int.right,
            //up
            pos + Vector2Int.up,
            //down
            pos + Vector2Int.down,
            //diagonals
            pos + Vector2Int.left + Vector2Int.up,
            pos + Vector2Int.up + Vector2Int.right,
            pos + Vector2Int.right + Vector2Int.down,
            pos + Vector2Int.down + Vector2Int.left,
        };

        List<Vector2Int> neighbours = new List<Vector2Int>();

        foreach(var pn in potentialNeighbours) {
            if(pn.x >= 0 && pn.x < gridSize.x
                && pn.y >= 0 && pn.y < gridSize.y
                && !visited.Contains(pn)) {
                    neighbours.Add(pn);
            }
        }

        return neighbours;
    }

    int ComputePathLenghtFrom(Vector2Int pos, Vector2Int[,] neighboursGrid, Vector2Int newPotentialDirection) {
        Vector2Int currentPos = pos + newPotentialDirection;
        int pathLength = 1;
        while(!currentPos.Equals(riverStart)) {
            currentPos += neighboursGrid[currentPos.x, currentPos.y];
            pathLength++;
        }

        return pathLength;
    }

    List<float3> GetKnotsFromNeighboursGrid(Vector2Int[,] neighboursGrid) {
        List<float3> knots = new List<float3>();
        Vector2Int currentNode = riverEnd;
        while(!currentNode.Equals(riverStart)) {
            rg.road_type_grid[currentNode.x, currentNode.y] = RoadType.Obstacle;

            knots.Add(ConvertToFloat3(currentNode) * tileSize);

            currentNode += neighboursGrid[currentNode.x, currentNode.y];
        }
        knots.Add(ConvertToFloat3(riverStart) * tileSize);

        return knots;
    }
}
