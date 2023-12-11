using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class TerrainAdjusterRuntime : MonoBehaviour
{
    public float brushFallOff = 0.3f;
    float[,] originalTerrainHeights;

    void Start()
    {
        //SaveOriginalTerrainHeights();
    }

    public void changeTerrain(List<List<Vector3>> mpoint, Terrain currentTerrain)
    {
        SaveOriginalTerrainHeights(currentTerrain);

        Vector3 terrainPosition = currentTerrain.gameObject.transform.position;
        TerrainData terrainData = currentTerrain.terrainData;

        // both GetHeights and SetHeights use normalized height values, where 0.0 equals to terrain.transform.position.y in the world space and 1.0 equals to terrain.transform.position.y + terrain.terrainData.size.y in the world space
        // so when using GetHeight you have to manually divide the value by the Terrain.activeTerrain.terrainData.size.y which is the configured height ("Terrain Height") of the terrain.
        float terrainMin = currentTerrain.transform.position.y + 0f;
        float terrainMax = currentTerrain.transform.position.y + currentTerrain.terrainData.size.y;
        float totalHeight = terrainMax - terrainMin;

        int w = terrainData.heightmapResolution;
        int h = terrainData.heightmapResolution;

        // clone the original data, the modifications along the path are based on them
        float[,] allHeights = originalTerrainHeights.Clone() as float[,];

        // the blur radius values being used for the various passes
        int[] initialPassRadii = { 15, 7, 2 };



        //List<List<Vector3>> mpoints = new List<List<Vector3>>(mpoint);




        List<List<Vector3>> testListCopy = new List<List<Vector3>>(mpoint.Count);
        foreach (List<Vector3> innerList in mpoint)
        {
            testListCopy.Add(new List<Vector3>(innerList));
        }



        for (int pass = 0; pass < initialPassRadii.Length; pass++)
        {
            int radius = initialPassRadii[pass];
            
            foreach (List< Vector3 > list in testListCopy)
            {
                list.Sort((a, b) => -a.y.CompareTo(b.y));
                Vector3[] points = list.ToArray();
                foreach (var point in points)
                {

                    float targetHeight = (point.y - terrainPosition.y) / totalHeight;

                    int centerX = (int)(point.z);
                    int centerY = (int)(point.x);

                    AdjustTerrain(allHeights, radius, centerX, centerY, targetHeight);

                }
            }
        }
        currentTerrain.terrainData.SetHeights(0, 0, allHeights);
    }


    void SaveOriginalTerrainHeights(Terrain currentTerrain)
    {
        TerrainData terrainData = currentTerrain.terrainData;

        int w = terrainData.heightmapResolution;
        int h = terrainData.heightmapResolution;

        originalTerrainHeights = terrainData.GetHeights(0, 0, w, h);
    }

    void Update()
    {
    }

    /*
    void ShapeTerrain(Terrain currentTerrain, spline Spline )
    {

        Vector3 terrainPosition = currentTerrain.gameObject.transform.position;
        TerrainData terrainData = currentTerrain.terrainData;

        // both GetHeights and SetHeights use normalized height values, where 0.0 equals to terrain.transform.position.y in the world space and 1.0 equals to terrain.transform.position.y + terrain.terrainData.size.y in the world space
        // so when using GetHeight you have to manually divide the value by the Terrain.activeTerrain.terrainData.size.y which is the configured height ("Terrain Height") of the terrain.
        float terrainMin = currentTerrain.transform.position.y + 0f;
        float terrainMax = currentTerrain.transform.position.y + currentTerrain.terrainData.size.y;
        float totalHeight = terrainMax - terrainMin;

        int w = terrainData.heightmapResolution;
        int h = terrainData.heightmapResolution;

        // clone the original data, the modifications along the path are based on them
        float[,] allHeights = originalTerrainHeights.Clone() as float[,];

        // the blur radius values being used for the various passes
        int[] initialPassRadii = { 15, 7, 2 };

        for (int pass = 0; pass < initialPassRadii.Length; pass++)
        {
            int radius = initialPassRadii[pass];

            // points as vertices, not equi-distant
            Vector3[] vertexPoints = currentPathCreator.path.vertices;

            // equi-distant points
            List<Vector3> distancePoints = new List<Vector3>();

            // spacing along the array, can speed up the loops
            float arrayIterationSpacing = 1;

            for (float t = 0; t <= currentPathCreator.path.length; t += arrayIterationSpacing)
            {
                Vector3 point = currentPathCreator.path.GetPointAtDistance(t, PathCreation.EndOfPathInstruction.Stop);

                distancePoints.Add(point);
            }

            // sort by height reverse
            // sequential height raising would just lead to irregularities, ie when a higher point follows a lower point
            // we need to proceed from top to bottom height
            distancePoints.Sort((a, b) => -a.y.CompareTo(b.y));

            Vector3[] points = distancePoints.ToArray();

            foreach (var point in points)
            {

                float targetHeight = (point.y - terrainPosition.y) / totalHeight;

                int centerX = (int)(currentPathCreator.transform.position.z + point.z);
                int centerY = (int)(currentPathCreator.transform.position.x + point.x);

                AdjustTerrain(allHeights, radius, centerX, centerY, targetHeight);

            }
        }

        currentTerrain.terrainData.SetHeights(0, 0, allHeights);
    }
    */

    private void AdjustTerrain(float[,] heightMap, int radius, int centerX, int centerY, float targetHeight)
    {
        float deltaHeight = targetHeight - heightMap[centerX, centerY];
        int sqrRadius = radius * radius;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int sqrDstFromCenter = offsetX * offsetX + offsetY * offsetY;

                // check if point is inside brush radius
                if (sqrDstFromCenter <= sqrRadius)
                {
                    // calculate brush weight with exponential falloff from center
                    float dstFromCenter = Mathf.Sqrt(sqrDstFromCenter);
                    float t = dstFromCenter / radius;
                    float brushWeight = Mathf.Exp(-t * t / brushFallOff);

                    // raise terrain
                    int brushX = centerX + offsetX;
                    int brushY = centerY + offsetY;

                    if (brushX >= 0 && brushY >= 0 && brushX < width && brushY < height)
                    {
                        heightMap[brushX, brushY] += deltaHeight * brushWeight;

                        // clamp the height
                        if (heightMap[brushX, brushY] > targetHeight)
                        {
                            heightMap[brushX, brushY] = targetHeight;
                        }
                    }
                }
            }
        }
    }
}