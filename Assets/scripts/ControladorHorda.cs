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
    
    //Luzes
    public GameObject luzes;
    //Quadros
    public GameObject quadros;

    //Emoções
    public Material negacao;
    public Material raiva;
    public Material barganha;
    public Material depressao;
    public Material aceitação;
    public Material insanidade;

    public int horda = 1; //Aqui que a magia acontece
    private Light[] todasAsLuzes;
    private Renderer[] todasOsQuadros;

    void Start()
    {
        spawner.OnHordeCompleted += OnHordeEnd;

        //Transform luzesParent = luzes.transform;
        todasAsLuzes = luzes.transform.GetComponentsInChildren<Light>();

        //Transform quadrosParent = quadros.transform;
        todasOsQuadros = quadros.transform.GetComponentsInChildren<Renderer>();

        //Joga a primeira horda para começar e depois vai para a magia
        //HORDA 1
        ZombieSpawnInfo[] horde = new ZombieSpawnInfo[]
        {
            new ZombieSpawnInfo { prefab = zombie, count = 10}
        };
        StartHorde(horde);
    }

    void OnHordeEnd()
    {

        Debug.Log("Acabou a horda?");

        if(spawner.currentZombies != 0)
        {
            Debug.Log("Não");
            //se não acabou a hora ainda ele vai se invocar daqui a 1 segundo e verificar denovo
            Invoke(nameof(OnHordeEnd), 1f);
            return;
        }

        Debug.Log("Siiiim");

        horda += 1;
        //NewHorde();
        //espera 10 segundos para chamar
        Invoke(nameof(NewHorde), 10f);
    }

    void NewHorde()
    {
        int total = horda * 5;  // total de zumbis da horda

        int qtd1 = 0;
        int qtd2 = 0;
        int qtd3 = 0;
        int restante = 0;

        Debug.Log("horda: " + horda);

        switch (horda)
        {
            case 5: //Apresenta novo tipo de zombie e spawna alguns dele
                
                qtd1 = 0;
                
                qtd2 = 30;
                
                qtd3 = 0;
                break;
            
            case 10: //Apresenta novo tipo de zombie e spawna alguns dele
                
                qtd1 = 0;
                
                qtd2 = 0;
                
                qtd3 = 30;
                break;

            case 15: //Apagaremos as luzes (muda as fotos do palhaço tbm mas esse ta por outra hora)
                ChangeLightColor(Color.black);
                ChangeClownPhotos(negacao);

                qtd1 = 25;
                
                qtd2 = 40;
                
                qtd3 = 10;
                break;

            case 20: //Muda para vermelho
                ChangeLightColor(Color.red);
                ChangeClownPhotos(raiva);
                
                qtd1 = 25;
                
                qtd2 = 40;
                
                qtd3 = 10;
                break;
            
            case 30: //Muda para verde
                ChangeLightColor(Color.green);
                ChangeClownPhotos(barganha);
                
                qtd1 = 30;
                
                qtd2 = 90;
                
                qtd3 = 30;
                break;
            
            case 40: //Muda para azul
                ChangeLightColor(Color.blue);
                ChangeClownPhotos(depressao);
                
                qtd1 = 80;
                
                qtd2 = 100;
                
                qtd3 = 20;
                break;

            case 50: //Muda para amarelo
                ChangeLightColor(Color.yellow);
                ChangeClownPhotos(aceitação);
                
                qtd1 = 50;
                
                qtd2 = 150;
                
                qtd3 = 50;
                break;
            
            case 60: //Insane
                //vou mudar aleatoriamente a cor
                ChangeClownPhotos(insanidade);
                
                qtd1 = 100;
                
                qtd2 = 100;
                
                qtd3 = 100;
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

    void StartHorde(ZombieSpawnInfo[] horde)
    {
        /*ZombieSpawnInfo[] horde = new ZombieSpawnInfo[]
        {
            new ZombieSpawnInfo { prefab = zombie, count = 5 },
            new ZombieSpawnInfo { prefab = zombieRapido, count = 3 },
            new ZombieSpawnInfo { prefab = zombieRastejante, count = 1 }
        };*/

        spawner.StartHorde(horde);
    }

    void ChangeLightColor(Color cor)
    {
        //Transform luzesParent = GameObject.Find("Luzes").transform;
        //Light[] todasAsLuzes = luzesParent.GetComponentsInChildren<Light>();
        todasAsLuzes = luzes.transform.GetComponentsInChildren<Light>();

        if (cor == Color.black)
        {
            foreach (Light l in todasAsLuzes)
            {
                l.enabled = false;
            }
        }
        else
        {
            foreach (Light l in todasAsLuzes)
            {
                l.enabled = true;
                l.color = cor;
            }
        }
    }

    public void ChangeClownPhotos(Material novoMaterial)
    {
        //Transform quadrosParent = GameObject.Find("Quadros").transform;
        //Renderer[] renderers = quadrosParent.GetComponentsInChildren<Renderer>();
        todasOsQuadros = quadros.transform.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in todasOsQuadros)
        {
            //r.material = novoMaterial;
            var material = r.materials;
            material[1] = novoMaterial;
            r.materials = material;
        }
    }

}
