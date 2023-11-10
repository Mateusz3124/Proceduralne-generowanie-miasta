using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject roadPrefab; 
    public int roadWidth = 10; 
    public int roadLength = 20; 
    public float segmentSize = 1.0f;
    public int repeatNumber = 2;


    void Start()
    {
        GameObject myPlane= GameObject.FindWithTag("Plane");
        GenerateRoad(myPlane);
    }

    void GenerateRoad(GameObject myPlane)
    {
        int[] cube = new int[] { 0, 0 , roadWidth, roadLength };
        //                       x   y   x               y

        division(cube, 0, myPlane);

    }

    void division(int [] cube, int counter, GameObject myPlane)
    {
        if(counter != repeatNumber)
        {
            
            System.Random random = new System.Random();
            int counterer = counter + 1;
            int horizontal = random.Next(cube[0]+1, cube[2]);
            int vertical1 = random.Next(cube[1]+1, cube[3]);
            int vertical2 = random.Next(cube[1]+1, cube[3]);

            for(int i = cube[1]; i < cube[3]; i++)
            {
                Vector3 position = new Vector3(horizontal* segmentSize, 0.0f, i * segmentSize);
                GameObject roadSegment = Instantiate(roadPrefab, position, Quaternion.identity); 
                roadSegment.transform.SetParent(myPlane.transform); 
            }
            for (int i = cube[0]; i < horizontal; i++)
            {
                Vector3 position = new Vector3(i * segmentSize, 0.0f, vertical1 * segmentSize);
                GameObject roadSegment = Instantiate(roadPrefab, position, Quaternion.identity);
                roadSegment.transform.SetParent(myPlane.transform);
            }
            for (int i = horizontal; i < cube[2]; i++)
            {
                Vector3 position = new Vector3(i * segmentSize, 0.0f, vertical2 * segmentSize);
                GameObject roadSegment = Instantiate(roadPrefab, position, Quaternion.identity);
                roadSegment.transform.SetParent(myPlane.transform);
            }
            int[] cube1 = new int[] { cube[0], cube[1], horizontal, vertical1 };
            int[] cube2 = new int[] { cube[0], vertical1, horizontal, cube[3] };
            int[] cube3 = new int[] { horizontal, cube[0], cube[2], vertical2 };
            int[] cube4 = new int[] { horizontal, vertical2, cube[2], cube[3] };

            division(cube1, counterer, myPlane);
            division(cube2, counterer, myPlane);
            division(cube3, counterer, myPlane);
            division(cube4, counterer, myPlane);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
