using System.Collections.Generic;
using UnityEngine;
using System; // Required for Action and Func

public class EnemySpawner
{
    private List<GameObject> enemies;
    private string[] currentEnemyNames; // Renamed to clearly indicate it's internal and current
    private GameObject[] enemyPrefabs;
    private Transform[] enemySpawnPoints;

    public EnemySpawner(List<GameObject> enemies, GameObject[] enemyPrefabs, Transform[] enemySpawnPoints)
    {
        this.enemies = enemies;
        this.enemyPrefabs = enemyPrefabs;
        this.enemySpawnPoints = enemySpawnPoints;
        currentEnemyNames = new string[0]; // Initialize as empty
    }

    public void SpawnWaveEnemies(int currentWave, float waveProgressionMultiplier, float enemyMaxHealth, List<float> currentEnemyHealths)
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
                GameObject.Destroy(enemy);
        }
        enemies.Clear();
        currentEnemyHealths.Clear();

        int enemiesToSpawn;
        if (currentWave <= 3) { enemiesToSpawn = UnityEngine.Random.Range(1, 3); }
        else { enemiesToSpawn = UnityEngine.Random.Range(2, 5); } // Can now be up to 4
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, enemySpawnPoints.Length);

        float scaledHealth = enemyMaxHealth * Mathf.Pow(waveProgressionMultiplier, currentWave - 1);
        Array.Resize(ref currentEnemyNames, enemiesToSpawn);

        // --- FIX: REVISED SPAWNING LOGIC ---
        List<GameObject> availablePrefabs = new List<GameObject>(enemyPrefabs);
        List<Transform> finalSpawnPoints = new List<Transform>();

        // Separate primary and overflow spawn points
        List<Transform> primaryPoints = new List<Transform>();
        for (int i = 0; i < 3 && i < enemySpawnPoints.Length; i++)
        {
            primaryPoints.Add(enemySpawnPoints[i]);
        }

        // Shuffle the primary points for randomness within the first 3 slots
        for (int i = 0; i < primaryPoints.Count; i++)
        {
            Transform temp = primaryPoints[i];
            int randomIndex = UnityEngine.Random.Range(i, primaryPoints.Count);
            primaryPoints[i] = primaryPoints[randomIndex];
            primaryPoints[randomIndex] = temp;
        }

        // Add shuffled primary points to the final list
        finalSpawnPoints.AddRange(primaryPoints);

        // If a 4th enemy needs to be spawned, add the 4th spawn point
        if (enemiesToSpawn == 4 && enemySpawnPoints.Length >= 4)
        {
            finalSpawnPoints.Add(enemySpawnPoints[3]);
        }


        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (availablePrefabs.Count == 0)
            {
                availablePrefabs.AddRange(enemyPrefabs); // Refresh list to allow duplicates if needed
            }

            int randomPrefabIndex = UnityEngine.Random.Range(0, availablePrefabs.Count);
            GameObject prefabToSpawn = availablePrefabs[randomPrefabIndex];
            availablePrefabs.RemoveAt(randomPrefabIndex);

            // Use the ordered and shuffled list of final spawn points
            Transform spawnPoint = finalSpawnPoints[i];

            GameObject newEnemy = GameObject.Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

            // ... (rest of the spawning logic is the same)
            EnemyAI enemyAI = newEnemy.GetComponent<EnemyAI>();
            if (enemyAI != null) { enemyAI.enabled = true; }
            Animator animator = newEnemy.GetComponent<Animator>();
            if (animator != null) { animator.enabled = true; }
            enemies.Add(newEnemy);
            currentEnemyHealths.Add(scaledHealth);
            currentEnemyNames[i] = newEnemy.name.Replace("(Clone)", "").Trim();
        }
    }

    public string[] GetEnemyNames()
    {
        return currentEnemyNames;
    }
}