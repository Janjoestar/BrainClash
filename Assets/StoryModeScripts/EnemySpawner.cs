using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner
{
    private List<GameObject> enemies;
    private string[] enemyNames;
    private GameObject[] enemyPrefabs;
    private Transform[] enemySpawnPoints;

    public EnemySpawner(List<GameObject> enemies, string[] enemyNames, GameObject[] enemyPrefabs, Transform[] enemySpawnPoints)
    {
        this.enemies = enemies;
        this.enemyNames = enemyNames;
        this.enemyPrefabs = enemyPrefabs;
        this.enemySpawnPoints = enemySpawnPoints;
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
            }
        }

        if (enemyNames.Length < enemies.Count)
        {
            System.Array.Resize(ref enemyNames, enemies.Count);
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            enemyNames[i] = enemies[i].name.Replace("(Clone)", "").Trim();
        }
    }

    public string[] GetEnemyNames()
    {
        return enemyNames;
    }
}