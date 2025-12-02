using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZombieSpawnInfo
{
    public GameObject prefab;
    public int count;
}

public class ZombieSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public float spawnInterval = 0.5f;
    public int maxZombies = 10;

    public int currentZombies = 0;
    
    Coroutine hordeCoroutine;

    // Callback para avisar quando a horda terminou
    public Action OnHordeCompleted;

    // Inicia a horda: recebe um array [prefab, qtd] que você monta no controlador
    public void StartHorde(ZombieSpawnInfo[] horde)
    {
        if (hordeCoroutine != null) StopCoroutine(hordeCoroutine);
        hordeCoroutine = StartCoroutine(SpawnHordeCoroutine(horde));
    }

    IEnumerator SpawnHordeCoroutine(ZombieSpawnInfo[] horde)
    {
        // achata a matriz em uma lista de prefabs (repetidos conforme qtd)
        var spawnList = new List<GameObject>();
        foreach (var info in horde)
            for (int i = 0; i < info.count; i++)
                spawnList.Add(info.prefab);

        // loop único pelo tamanho total da horda
        for (int i = 0; i < spawnList.Count; i++)
        {
            // espera até liberar espaço respeitando maxZombies
            while (currentZombies >= maxZombies)
                yield return null; // espera um frame

            SpawnSingle(spawnList[i]);

            if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
            else yield return null;
        }

        hordeCoroutine = null;
        OnHordeCompleted?.Invoke();
    }

    public void SpawnSingle(GameObject prefab)
    {
        // instancia no ponto aleatório (assume que spawnPoints tem pelo menos 1 elemento)
        int idx = UnityEngine.Random.Range(0, spawnPoints.Length);
        Transform sp = spawnPoints[idx];
        Instantiate(prefab, sp.position, sp.rotation);
        currentZombies++;
    }

    // Deve ser chamada pelo seu código quando um zumbi morrer / for despawnado
    public void NotifyZombieRemoved()
    {
        currentZombies = Mathf.Max(0, currentZombies - 1);
    }

}
