using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Configuração do Spawn")]
    public GameObject zombiePrefab;     // arrasta o prefab aqui no Inspector
    public Transform[] spawnPoints;     // locais possíveis de spawn
    public float spawnInterval = 3f;    // tempo entre spawns
    public int maxZombies = 10;         // limite simultâneo

    private int currentZombies = 0;

    void Start()
    {
        InvokeRepeating(nameof(SpawnZombie), 0f, spawnInterval);
    }

    void SpawnZombie()
    {
        if (currentZombies >= maxZombies) return;

        // escolhe ponto de spawn aleatório
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // instancia o zumbi
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);

        currentZombies++;
    }

    void OnZombieDeath()
    {
        currentZombies--;
    }
}
