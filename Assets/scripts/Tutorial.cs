using UnityEngine;
using System.Collections.Generic;
using DynamicMeshCutter;


public class Tutorial : MonoBehaviour
{
    //Zombies
    public ZombieSpawner spawner;
    public GameObject zombie;

    private List<Joycon> joycons;
    public int jcIndex = 0;

    public VariavelGlobal variaveisGlobais;

    public SaiDoTutorial saiTutorial;

    void Start()
    {
        zombie.SetActive(true);
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();
    }

    void Update()
    {
        Joycon j = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;

        if (Input.GetKeyDown(KeyCode.Z) || j.GetButtonDown(Joycon.Button.SHOULDER_1))
        {
            SpawnZombie();
        }
        if (j.GetButtonDown(Joycon.Button.DPAD_LEFT)) {
            saiTutorial.Iniciar();
        }

    }

    void SpawnZombie()
    {
        spawner.SpawnSingle(zombie);
    }

}
