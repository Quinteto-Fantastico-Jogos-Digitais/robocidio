using UnityEngine;


public class Tutorial : MonoBehaviour
{
    //Zombies
    public ZombieSpawner spawner;
    public GameObject zombie;

    public VariavelGlobal variaveisGlobais;

    void Start()
    {
        zombie.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SpawnZombie();
        }
    }

    void SpawnZombie()
    {
        spawner.SpawnSingle(zombie);
    }

}
