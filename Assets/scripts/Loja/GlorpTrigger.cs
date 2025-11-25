using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class GlorpTrigger : MonoBehaviour
{
    public Transform playerBody;       // optional: used to evaluate local-space clamps
    public VariavelGlobal variaveisGlobais;

    private bool colidindo = false;

    [Tooltip("Arraste aqui o container que contém as armas já instanciadas na cena (filhos).")]
    public Transform container; // WeaponContainer (tem as armas como filhos)

    [Tooltip("Índice inicial a ativar")]
    public int startIndex = 0;

    List<GameObject> items;
    int currentIndex = -1;

    public bool SwitchClicou = false;

    void Awake()
    {
        /*if (container == null)
        {
            Debug.LogError("PreinstancedSwitcher: container não atribuído.");
            return;
        }

        // monta lista a partir dos filhos (ordem como estão na hierarquia)
        items = new List<GameObject>(container.childCount);
        for (int i = 0; i < container.childCount; i++)
            items.Add(container.GetChild(i).gameObject);

        // opcional: garantir todos desativados (com exceção do startIndex)
        for (int i = 0; i < items.Count; i++)
            items[i].SetActive(false);

        if (startIndex >= 0 && startIndex < items.Count)
            Activate(startIndex);*/
    }

    void Start()
    {
        if (container == null)
        {
            Debug.LogError("PreinstancedSwitcher: container não atribuído.");
            return;
        }

        // monta lista a partir dos filhos (ordem como estão na hierarquia)
        items = new List<GameObject>(container.childCount);
        for (int i = 0; i < container.childCount; i++)
            items.Add(container.GetChild(i).gameObject);

        // opcional: garantir todos desativados (com exceção do startIndex)
        for (int i = 0; i < items.Count; i++)
            items[i].SetActive(false);

        if (startIndex >= 0 && startIndex < items.Count)
            Activate(startIndex);
    }

    /// <summary>Ativa item por índice (desativa o anterior).</summary>
    public void Activate(int index)
    {
        if (items == null || items.Count == 0) return;
        if (index < 0 || index >= items.Count) 
        {
            Debug.LogWarning("Activate: index fora do range: " + index);
            return;
        }

        if (index == currentIndex) return; // nada a fazer

        // desativa somente o anterior (se houver)
        if (currentIndex >= 0 && currentIndex < items.Count)
            items[currentIndex].SetActive(false);

        // ativa o novo
        items[index].SetActive(true);

        currentIndex = index;
    }

    /// <summary>Ativa um item aleatório.</summary>
    public void ActivateRandom()
    {
        if (items == null || items.Count == 0) return;

        if (!temDinheiro())
        {
            variaveisGlobais.setTextoAux("Você não tem dinheiro para pagar Glorp.");
            return;
        }
        
        if (variaveisGlobais.glorpCooldown == false)
        {
            variaveisGlobais.setTextoAux("Glorp em espera. [Espere a proxima horda]");
            return;
        }

        Activate(Random.Range(0, items.Count));
        variaveisGlobais.glorpCooldown = false;
        variaveisGlobais.SubtraiPontos(1500);
    }

    void Update()
    {
        if (colidindo)
        {
            if (Input.GetKeyDown(KeyCode.E) || SwitchClicou)
            {
                ActivateRandom();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!this.enabled) return;

        int tipoControle = PlayerPrefs.GetInt("tipoControle", 0);

        Debug.Log("Colidi com: " + other.gameObject.name);

        //Se for inimigo chama a função de morrer
        if (other.gameObject.name == playerBody.gameObject.name)
        {
            if (tipoControle == 0)
            {
                variaveisGlobais.setTextoAux("Pressione <color=#FFFF00>E</color> para tentar a sorte no Glorp. [Custa 1500]");
            }
            else
            {
                variaveisGlobais.setTextoAux("Pressione <color=#FFFF00>A</color> para tentar a sorte no Glorp. [Custa 1500]");
            }
            
            colidindo = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        if (!this.enabled) return;

        Debug.Log("Colidi com: " + other.gameObject.name);

        //Se for inimigo chama a função de morrer
        if (other.gameObject.name == playerBody.gameObject.name)
        {
            variaveisGlobais.setTextoAux("");
            colidindo = false;
        }
    }

    public bool temDinheiro()
    {
        if (variaveisGlobais.pontos < 1500) return false;
        return true;
    }

    
}
