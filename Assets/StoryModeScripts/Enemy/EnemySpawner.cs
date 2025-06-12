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
        if (currentWave <= 3)
        {
            enemiesToSpawn = UnityEngine.Random.Range(1, 3);
        }
        else
        {
            enemiesToSpawn = UnityEngine.Random.Range(2, 5);
        }
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, enemySpawnPoints.Length);

        float scaledHealth = enemyMaxHealth * Mathf.Pow(waveProgressionMultiplier, currentWave - 1);

        // Resize currentEnemyNames to match the number of enemies to spawn
        Array.Resize(ref currentEnemyNames, enemiesToSpawn);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (i < enemySpawnPoints.Length)
            {
                int randomPrefabIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
                GameObject newEnemy = GameObject.Instantiate(enemyPrefabs[randomPrefabIndex],
                                                             enemySpawnPoints[i].position,
                                                             Quaternion.identity);
                enemies.Add(newEnemy);
                currentEnemyHealths.Add(scaledHealth);
                currentEnemyNames[i] = newEnemy.name.Replace("(Clone)", "").Trim(); // Populate the internal names array
            }
        }
    }

    public string[] GetEnemyNames()
    {
        return currentEnemyNames;
    }
}