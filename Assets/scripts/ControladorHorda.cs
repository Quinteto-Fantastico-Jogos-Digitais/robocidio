using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ControladorHorda : MonoBehaviour
{
    //Zombies
    public ZombieSpawner spawner;
    public GameObject zombie;
    public GameObject zombieRapido;
    public GameObject zombieRastejante;

    public GameObject Horda_Inicio;
    public GameObject Horda_5;
    public GameObject Horda_10;
    public GameObject Horda_15;
    public GameObject Horda_20;
    public GameObject Horda_25;
    public GameObject Horda_30;
    public GameObject Horda_End;
    public GameObject Derrota;

    private int horda = 1; //Aqui que a magia acontece

    public VariavelGlobal variaveisGlobais;

    void Start()
    {
        spawner.OnHordeCompleted += OnHordeEnd;

        zombie.SetActive(true);
        zombieRapido.SetActive(true);
        zombieRastejante.SetActive(true);

        //StartHorde(horde);
        Invoke(nameof(StartHorde), 10f);
    }

    void OnHordeEnd()
    {
        if(spawner.currentZombies != 0)
        {
            //se não acabou a hora ainda ele vai se invocar daqui a 1 segundo e verificar denovo
            Invoke(nameof(OnHordeEnd), 1f);
            return;
        }

        horda += 1;

        //espera 30 segundos para chamar
        Invoke(nameof(NewHorde), 15f);
    }

    void NewHorde()
    {
        variaveisGlobais.SomaHorda();
        variaveisGlobais.glorpCooldown = true;

        int total = horda * 3;  // total de zumbis da horda

        int qtd1 = 0;
        int qtd2 = 0;
        int qtd3 = 0;
        int restante = 0;

        Debug.Log("horda: " + horda);

        switch (horda)
        {
            case 5: //Apresenta novo tipo de zombie e spawna alguns dele
                Horda_Inicio.SetActive(false);
                Horda_5.SetActive(true);
                
                qtd1 = UnityEngine.Random.Range(0, total + 1);
                restante = total - qtd1;
                
                qtd2 = restante;

                qtd3 = 0;
                break;
            
            case 10: //Apresenta novo tipo de zombie e spawna alguns dele
                Horda_5.SetActive(false);
                Horda_10.SetActive(true);

                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;

            case 15: //Apagaremos as luzes (muda as fotos do palhaço tbm mas esse ta por outra hora)
                Horda_10.SetActive(false);
                Horda_15.SetActive(true);

                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;

            case 20: //Muda para vermelho
                Horda_15.SetActive(false);
                Horda_20.SetActive(true);

                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;
            
            case 25: //Muda para verde
                Horda_20.SetActive(false);
                Horda_25.SetActive(true);

                
                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;
            
            case 30: //Muda para azul
                Horda_25.SetActive(false);
                Horda_30.SetActive(true);

                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;

            case 35: //Muda para amarelo
                Horda_30.SetActive(false);
                Horda_End.SetActive(true);

                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;
            
            case > 60:
                total = horda * 10;
                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;

            case > 10: //Distribui aleatoriamente entre os 3 tipos
                qtd1 = UnityEngine.Random.Range(0, total + 1);

                restante = total - qtd1;
                qtd2 = UnityEngine.Random.Range(0, restante + 1);

                qtd3 = total - (qtd1 + qtd2);
                break;

            case > 5: //Aqui ocorre para chamar entre os dois tipos rapido e normal
                
                qtd1 = UnityEngine.Random.Range(0, total + 1);
                restante = total - qtd1;
                
                qtd2 = restante;

                qtd3 = 0;
                break;

            default: //So vai entrar aqui enquanto tiver só um tipo de zombie

                qtd1 = total;

                qtd2 = 0;

                qtd3 = 0;
                break;
        }

        ZombieSpawnInfo[] horde = new ZombieSpawnInfo[]
        {
            new ZombieSpawnInfo { prefab = zombie, count = qtd1},
            new ZombieSpawnInfo { prefab = zombieRapido, count = qtd2},
            new ZombieSpawnInfo { prefab = zombieRastejante, count = qtd3}
        };

        StartHorde(horde);

    }

    //void StartHorde(ZombieSpawnInfo[] horde)
    void StartHorde()
    {
        //Chama na primeira horda
        ZombieSpawnInfo[] horde = new ZombieSpawnInfo[]
        {
            new ZombieSpawnInfo { prefab = zombie, count = 10}
        };

        spawner.StartHorde(horde);
    }

    void StartHorde(ZombieSpawnInfo[] horde)
    {
        spawner.StartHorde(horde);
    }

    public void CallGameOver()
    {
        Horda_Inicio.SetActive(false);
        Horda_5.SetActive(false);
        Horda_10.SetActive(false);
        Horda_15.SetActive(false);
        Horda_20.SetActive(false);
        Horda_25.SetActive(false);
        Horda_30.SetActive(false);
        Horda_End.SetActive(false);
        Derrota.SetActive(true);
    }

}
