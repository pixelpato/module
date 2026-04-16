using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Transform player;
    public GameObject rockPrefab;

    [Header("Spacing")]
    public float rockWidth = 10f; // width of your rock prefab

    [Header("Height Settings")]
    public float maxStepHeight = 2f; // max Y difference between neighbors
    public float minY = -5f;
    public float maxY = 10f; // "ceiling"

    [Header("Spawn Settings")]
    public int initialRocks = 10;
    public float spawnDistance = 30f;

    private List<Transform> rocks = new List<Transform>();

    private float leftMostX;
    private float rightMostX;

    void Start()
    {
        // Spawn initial chain centered around player
        float startX = Mathf.Floor(player.position.x / rockWidth) * rockWidth;

        float currentY = 0f;

        for (int i = -initialRocks / 2; i < initialRocks / 2; i++)
        {
            float x = startX + i * rockWidth;
            currentY = GetNextY(currentY);

            Transform rock = SpawnRock(new Vector3(x, currentY, 0));
            rocks.Add(rock);
        }

        UpdateBounds();
    }

    void Update()
    {
        // Spawn to the right
        if (player.position.x + spawnDistance > rightMostX)
        {
            SpawnRight();
        }

        // Spawn to the left
        if (player.position.x - spawnDistance < leftMostX)
        {
            SpawnLeft();
        }
    }

    void SpawnRight()
    {
        Transform last = rocks[rocks.Count - 1];

        float newX = last.position.x + rockWidth;
        float newY = GetNextY(last.position.y);

        Transform rock = SpawnRock(new Vector3(newX, newY, 0));
        rocks.Add(rock);

        rightMostX = newX;
    }

    void SpawnLeft()
    {
        Transform first = rocks[0];

        float newX = first.position.x - rockWidth;
        float newY = GetNextY(first.position.y);

        Transform rock = SpawnRock(new Vector3(newX, newY, 0));
        rocks.Insert(0, rock);

        leftMostX = newX;
    }

    Transform SpawnRock(Vector3 position)
    {
        GameObject obj = Instantiate(rockPrefab, position, Quaternion.identity);

        // Random Z rotation (2.5D style)
        float randomZ = Random.Range(0f, 360f);
        obj.transform.rotation = Quaternion.Euler(randomZ, randomZ, randomZ);

        return obj.transform;
    }

    float GetNextY(float previousY)
    {
        float deltaY = Random.Range(-maxStepHeight, maxStepHeight);
        float newY = previousY + deltaY;

        // Ceiling rule: if too high, force downward
        if (previousY >= maxY)
        {
            newY = previousY - Mathf.Abs(Random.Range(0f, maxStepHeight));
        }

        // Clamp overall bounds
        newY = Mathf.Clamp(newY, minY, maxY);

        return newY;
    }

    void UpdateBounds()
    {
        leftMostX = rocks[0].position.x;
        rightMostX = rocks[rocks.Count - 1].position.x;
    }
}