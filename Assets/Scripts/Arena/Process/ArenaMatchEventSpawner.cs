using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaMatchEventSpawner : MonoBehaviour
{
    [Header("Pickup Spawn")]
    [SerializeField] private List<GameObject> pickupPrefabs = new List<GameObject>();
    [SerializeField] private List<Transform> pickupSpawnPoints = new List<Transform>();
    [SerializeField] private int pickupCountAtMatchStart = 3;

    [Header("Hazard Spawn")]
    [SerializeField] private List<GameObject> hazardPrefabs = new List<GameObject>();
    [SerializeField] private List<Transform> hazardSpawnPoints = new List<Transform>();
    [SerializeField] private float hazardSpawnIntervalMin = 6f;
    [SerializeField] private float hazardSpawnIntervalMax = 10f;
    [SerializeField] private float hazardLifetimeMin = 5f;
    [SerializeField] private float hazardLifetimeMax = 8f;
    [SerializeField] private int maxHazardsAlive = 2;

    private List<GameObject> spawnedPickups = new List<GameObject>();
    private List<GameObject> spawnedHazards = new List<GameObject>();
    private Coroutine hazardRoutine;

    public void BeginMatchCycle()
    {
        ClearAllSpawnedObjects();
        SpawnInitialPickups();

        if (hazardRoutine != null)
        {
            StopCoroutine(hazardRoutine);
        }

        hazardRoutine = StartCoroutine(HazardLoopRoutine());
    }

    public void EndMatchCycle()
    {
        if (hazardRoutine != null)
        {
            StopCoroutine(hazardRoutine);
            hazardRoutine = null;
        }

        ClearAllSpawnedObjects();
    }

    private void SpawnInitialPickups()
    {
        List<Transform> availablePoints;
        int i;

        availablePoints = new List<Transform>(pickupSpawnPoints);
        ShuffleList(availablePoints);

        for (i = 0; i < pickupCountAtMatchStart && i < availablePoints.Count; i++)
        {
            GameObject prefab;
            GameObject instance;

            prefab = GetRandomPrefab(pickupPrefabs);

            if (prefab == null || availablePoints[i] == null)
            {
                continue;
            }

            instance = Instantiate(prefab, availablePoints[i].position, Quaternion.identity);
            spawnedPickups.Add(instance);
        }
    }

    private IEnumerator HazardLoopRoutine()
    {
        while (true)
        {
            float waitTime;

            waitTime = Random.Range(hazardSpawnIntervalMin, hazardSpawnIntervalMax);
            yield return new WaitForSeconds(waitTime);

            CleanupNulls();

            if (spawnedHazards.Count >= maxHazardsAlive)
            {
                continue;
            }

            SpawnOneHazard();
        }
    }

    private void SpawnOneHazard()
    {
        GameObject prefab;
        Transform point;
        GameObject instance;
        float lifetime;

        prefab = GetRandomPrefab(hazardPrefabs);
        point = GetRandomPoint(hazardSpawnPoints);

        if (prefab == null || point == null)
        {
            return;
        }

        instance = Instantiate(prefab, point.position, Quaternion.identity);
        spawnedHazards.Add(instance);

        lifetime = Random.Range(hazardLifetimeMin, hazardLifetimeMax);
        StartCoroutine(DestroyHazardAfterLifetime(instance, lifetime));
    }

    private IEnumerator DestroyHazardAfterLifetime(GameObject hazardObject, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (hazardObject != null)
        {
            spawnedHazards.Remove(hazardObject);
            Destroy(hazardObject);
        }
    }

    private void ClearAllSpawnedObjects()
    {
        int i;

        for (i = spawnedPickups.Count - 1; i >= 0; i--)
        {
            if (spawnedPickups[i] != null)
            {
                Destroy(spawnedPickups[i]);
            }
        }

        for (i = spawnedHazards.Count - 1; i >= 0; i--)
        {
            if (spawnedHazards[i] != null)
            {
                Destroy(spawnedHazards[i]);
            }
        }

        spawnedPickups.Clear();
        spawnedHazards.Clear();
    }

    private void CleanupNulls()
    {
        int i;

        for (i = spawnedPickups.Count - 1; i >= 0; i--)
        {
            if (spawnedPickups[i] == null)
            {
                spawnedPickups.RemoveAt(i);
            }
        }

        for (i = spawnedHazards.Count - 1; i >= 0; i--)
        {
            if (spawnedHazards[i] == null)
            {
                spawnedHazards.RemoveAt(i);
            }
        }
    }

    private GameObject GetRandomPrefab(List<GameObject> prefabs)
    {
        List<GameObject> validPrefabs;
        int i;

        validPrefabs = new List<GameObject>();

        for (i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null)
            {
                continue;
            }

            validPrefabs.Add(prefabs[i]);
        }

        if (validPrefabs.Count == 0)
        {
            return null;
        }

        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    private Transform GetRandomPoint(List<Transform> points)
    {
        List<Transform> validPoints;
        int i;

        validPoints = new List<Transform>();

        for (i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
            {
                continue;
            }

            validPoints.Add(points[i]);
        }

        if (validPoints.Count == 0)
        {
            return null;
        }

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    private void ShuffleList<T>(List<T> list)
    {
        int i;

        for (i = 0; i < list.Count; i++)
        {
            int swapIndex;
            T temp;

            swapIndex = Random.Range(i, list.Count);
            temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }
}
