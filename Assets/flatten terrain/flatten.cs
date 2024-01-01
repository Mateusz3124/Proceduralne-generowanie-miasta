using UnityEngine;
using UnityEngine.TerrainTools;
using Unity.Mathematics;
using System;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;
using TreeEditor;

public class Flatten : MonoBehaviour
{
    public Procedural_Terrain terrain;
    float terrainTileSize;

    //"Optional shaped ramp around perimeter."
    public AnimationCurve PerimeterRampCurve = AnimationCurve.Linear(0, 0, 1, 1);

    //Size of gaussian filter applied to change array. Set to zero for none"
    public int PerimeterRampDistance = 1;

    //"Use Perimeter Ramp Curve in lieu of direct gaussian smooth."
    public bool ApplyPerimeterRampCurve = false;

    // This extends the binary on/off blend stencil out by one pixel,
    // making one sheet at a time, then stacks (adds) them all together and
    // renormalizes them back to 0.0-1.0.
    //
    // it simultaneously takes the average of the "hitting" perimeter neighboring
    // heightmap cells and extends it outwards as it expands.
    //
    void GeneratePerimeterHeightRampAndFlange(float[,] heightMap, float[,] blendStencil, int distance)
    {
        int w = blendStencil.GetLength(0);
        int h = blendStencil.GetLength(1);

        // each stencil, expanded by one more pixel, before we restack them
        float[][,] stencilPile = new float[distance + 1][,];

        // where we will build the horizontal heightmap flange out
        float[,] extendedHeightmap = new float[w, h];

        // directonal table: 4-way and 8-way available
        int[] neighborXYPairs = new int[] {
			// compass directions first
			0, 1,
            1, 0,
            0, -1,
            -1, 0,
			// diagonals next
			1,1,
            -1,1,
            1,-1,
            -1,-1,
        };

        int neighborCount = 4;                  // 4 and 8 are supported from the table above

        float[,] source = blendStencil;         // this is NOT a copy! This is a reference!
        for (int n = 0; n <= distance; n++)
        {
            // add it to the pile BEFORE we expand it;
            // that way the first one is the original
            // input blendStencil.
            stencilPile[n] = source;

            // Debug: WritePNG( source, "pile-" + n.ToString());

            // this is gonna be an actual true deep copy of the stencil
            // as it stands now, and it will steadily grow outwards, but
            // each time it is always 0.0 or 1.0 cells, nothing in between.
            float[,] expanded = new float[w, h];
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    expanded[i, j] = source[i, j];
                }
            }

            // we have to quit so we don't further expand the flange heightmap
            if (n == distance)
            {
                break;
            }

            // Add one solid pixel around perimeter of the stencil.
            // Also ledge-extend the perimeter heightmap value for those
            // non-zero cells, not reducing them at all (they are like
            // flat flange going outwards that we need in order to later blend).
            //
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    if (source[i, j] == 0)
                    {
                        // serves as "hit" or not too
                        int count = 0;

                        // for average of neighboring heights
                        float height = 0.0f;

                        for (int neighbor = 0; neighbor < neighborCount; neighbor++)
                        {
                            int x = i + neighborXYPairs[neighbor * 2 + 0];
                            int y = j + neighborXYPairs[neighbor * 2 + 1];
                            if ((x >= 0) && (x < w) && (y >= 0) && (y < h))
                            {
                                // found a neighbor: we will:
                                //	- areally expand the stencil by this one pixel
                                //	- sample the neighbor height for the flange extension
                                if (source[x, y] != 0)
                                {
                                    height += heightMap[x, y];
                                    count++;
                                }
                            }
                        }

                        // extend the height of this cell by the average height
                        // of the neighbors that contained source stencil true
                        if (count > 0)
                        {
                            expanded[i, j] = 1.0f;

                            extendedHeightmap[i, j] = height / count;
                        }
                    }
                }
            }

            // Copy the new ledge back to the original heightmap.
            // WARNING: this is an "output" operation because it is
            // modifying the supplied input heightmap data, areally
            // adding around the edge by the pixels encountered.
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    var height = extendedHeightmap[i, j];

                    // only lift... this still allows us to lower terrain,
                    // since it is lifting from absolute zero to the altitude
                    // that we actually sensed at this hit neighbor pixels,
                    // and we need this unattenuated height for later blending.
                    if (height > 0)
                    {
                        heightMap[i, j] = height;
                    }

                    // zero it too, for next layer (might not be necessary??)
                    extendedHeightmap[i, j] = 0;
                }
            }

            // assign the source to this fresh copy
            source = expanded;          // shallow copy (reference)
        }

        // now tally the pile, summarizing each stack of 0/1 solid pixels,
        // copying it to to the stencil array passed in, which will change
        // its contents directly, and renormalize it back down to 0.0 to 1.0
        //
        // WARNING: this is also an output operation, as it modifies the
        // blendStencil inbound dataset
        //
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                float total = 0;
                for (int n = 0; n <= distance; n++)
                {
                    total += stencilPile[n][i, j];
                }

                total /= (distance + 1);

                blendStencil[i, j] = total;
            }
        }

        // Debug: WritePNG( blendStencil, "blend");
    }



    //main function of flattening the terriang base on detecting mesh using raycast
    public void changeTerrainWhenMesh()
    {
        terrain = GameObject.Find("Terrain").GetComponent<Procedural_Terrain>();
        TerrainData terData = terrain.terrain.terrainData;
        int Tw = terData.heightmapResolution;
        int Th = terData.heightmapResolution;
        var heightMapOriginal = terData.GetHeights(0, 0, Tw, Th);
        // where we do our work when we generate the new terrain heights
        var heightMapCreated = new float[heightMapOriginal.GetLength(0), heightMapOriginal.GetLength(1)];
        // for blending heightMapCreated with the heightMapOriginal to form
        var heightAlpha = new float[heightMapOriginal.GetLength(0), heightMapOriginal.GetLength(1)];

        terrainTileSize = terrain.width / (float)terrain.resolution;
        for (int x = 0; x < terrain.resolution; x++)
        {
            for (int z = 0; z < terrain.resolution; z++)
            {
                Vector3 positionInTerrain = new Vector3(x * terrainTileSize, 200f, z * terrainTileSize);
                float height = SampleRoadHeight(positionInTerrain);
                if(height != -1f)
                {
                    heightMapCreated[z, x] = (height-0.1f)/ terrain.depth;
                    heightAlpha[z, x] = 1.0f;
                }
            }
        }
        // now we might smooth things out a bit
        if (PerimeterRampDistance > 0)
        {
            // Debug: WritePNG( heightMapCreated, "height-0", true);
            // Debug: WritePNG( heightAlpha, "alpha-0", true);

            GeneratePerimeterHeightRampAndFlange(
                heightMap: heightMapCreated,
                blendStencil: heightAlpha,
                distance: PerimeterRampDistance);

            // Debug: WritePNG( heightMapCreated, "height-1", true);
            // Debug: WritePNG( heightAlpha, "alpha-1", true);
        }

        // apply the generated data (blend operation)
        for (int Tz = 0; Tz < Th; Tz++)
        {
            for (int Tx = 0; Tx < Tw; Tx++)
            {
                float fraction = heightAlpha[Tz, Tx];

                if (ApplyPerimeterRampCurve)
                {
                    fraction = PerimeterRampCurve.Evaluate(fraction);
                }

                heightMapOriginal[Tz, Tx] = Mathf.Lerp(
                    heightMapOriginal[Tz, Tx],
                    heightMapCreated[Tz, Tx],
                    fraction);
            }
        }

        terrain.terrain.terrainData.SetHeights(0, 0, heightMapOriginal);
    }

    void Start()
    {

    }
    // 
    float SampleRoadHeight(Vector3 position)
    {
        RaycastHit hit;
        int layerMask = 1 << 3;
        Ray rayUp = new Ray(position, Vector3.down);
        if (Physics.Raycast(rayUp, out hit, 200, layerMask))
        {
            // detect if road
            if (hit.collider != null && hit.collider.GetType() == typeof(MeshCollider))
            {
                return hit.point.y;
            }
        }
        return -1f;
    }
    // Update is called once per frame
    void Update()
    {

    }
}