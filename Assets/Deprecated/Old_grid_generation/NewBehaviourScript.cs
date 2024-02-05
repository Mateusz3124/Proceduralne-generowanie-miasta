using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject obstaclePrefab;
    public int startPointX = -100;
    public int startPointY = -100;
    public int roadWidth = 200;
    public int roadLength = 200;
    public int repeatNumber = 4;
    public int nmbOfObstacles = 5;

    private float segmentSize = 1.0f;
    private System.Random random;
    private List<Vector3> obstaclePositions;

    void Start()
    {
        random = new System.Random();
        obstaclePositions = new List<Vector3>();
        GameObject myPlane = GameObject.FindWithTag("Plane");

        GenerateObstacles(myPlane);
        GenerateRoad(myPlane);
    }

    void GenerateRoad(GameObject myPlane)
    {
        Vector2Int startPoint = new Vector2Int(startPointX, startPointY);
        Vector2Int endPoint = new Vector2Int(startPointX + roadWidth, startPointY + roadLength);
        Vector2Int[] cube = { startPoint, endPoint };

        division(cube, 0, myPlane);
    }

    void GenerateObstacles(GameObject myPlane)
    {
        for (int i = 0; i < nmbOfObstacles; i++)
        {
            int obstacleX = random.Next(startPointX, startPointX + roadWidth);
            int obstacleY = random.Next(startPointY, startPointY + roadLength);

            Vector3 obstaclePos = new Vector3(obstacleX * segmentSize, 0.0f, obstacleY * segmentSize);
            obstaclePositions.Add(obstaclePos);

            Instantiate(obstaclePrefab, obstaclePos, Quaternion.identity).transform.SetParent(myPlane.transform);
        }
    }

    void division(Vector2Int[] cube, int counter, GameObject myPlane)
    {
        if (counter == repeatNumber)
        {
            return;
        }

        int counterer = counter + 1;
        int horizontal = random.Next(cube[0].x, cube[1].x);
        int vertical1 = random.Next(cube[0].y, cube[1].y);
        int vertical2 = random.Next(cube[0].y, cube[1].y);

        GenerateRoadSegment(cube[0].y, cube[1].y, horizontal, true, myPlane);
        GenerateRoadSegment(cube[0].x, horizontal, vertical1, false, myPlane);
        GenerateRoadSegment(horizontal, cube[1].x, vertical2, false, myPlane);

        Vector2Int[] cube1 = { new Vector2Int(cube[0].x, cube[0].y), new Vector2Int(horizontal, vertical1) };
        Vector2Int[] cube2 = { new Vector2Int(cube[0].x, vertical1), new Vector2Int(horizontal, cube[1].y) };
        Vector2Int[] cube3 = { new Vector2Int(horizontal, cube[0].y), new Vector2Int(cube[1].x, vertical2) };
        Vector2Int[] cube4 = { new Vector2Int(horizontal, vertical2), new Vector2Int(cube[1].x, cube[1].y) };

        division(cube1, counterer, myPlane);
        division(cube2, counterer, myPlane);
        division(cube3, counterer, myPlane);
        division(cube4, counterer, myPlane);
    }

    void GenerateRoadSegment(int start, int end, int position, bool isVertical, GameObject myPlane)
    {
        if (CheckCollisionWithObstacles(position, isVertical))
        {
            return;
        }

        for (int i = start; i < end; i++)
        {
            Vector3 spawnPosition;
            if (isVertical)
            {
                spawnPosition = new Vector3(position * segmentSize, 0.0f, i * segmentSize);
            }
            else
            {
                spawnPosition = new Vector3(i * segmentSize, 0.0f, position * segmentSize);
            }
            GameObject roadSegment = Instantiate(roadPrefab, spawnPosition, Quaternion.identity);
            roadSegment.transform.SetParent(myPlane.transform);
        }
    }

    bool CheckCollisionWithObstacles(int position, bool isVertical)
    {
        float obstacleSize;
        if(isVertical) {
            obstacleSize = obstaclePrefab.transform.localScale.x;
            foreach (Vector3 obstaclePosition in obstaclePositions)
            {
                if (Math.Abs(position - obstaclePosition.x) <= (segmentSize + obstacleSize)/2)
                {
                    return true;
                }
            }
        }
        else {
            obstacleSize = obstaclePrefab.transform.localScale.z;
            foreach (Vector3 obstaclePosition in obstaclePositions)
            {
                if (Math.Abs(position - obstaclePosition.z) <= (segmentSize + obstacleSize)/2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
