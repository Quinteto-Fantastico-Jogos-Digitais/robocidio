using UnityEngine;
using System.Collections.Generic; // Necessário para usar Listas

public class ShopMechanichals : MonoBehaviour
{
    [Header("Referências")]
    public CoinCollection CarteiraPlayer;

    [Header("Configuração da Loja")]
    public List<ShopItemData> allPossibleItems; // Arraste TODOS os seus itens (ScriptableObjects) aqui
    public List<ShopItemUI> shopSlots; // Arraste os 3 slots de UI da sua loja aqui

    private List<ShopItemData> currentShopItems = new List<ShopItemData>();

    void Start()
    {
        // Popula a loja com itens aleatórios quando o jogo/fase começa
        GenerateRandomItems();
    }

    public void GenerateRandomItems()
    {
        currentShopItems.Clear();
        List<ShopItemData> availableItems = new List<ShopItemData>(allPossibleItems);

        // Garante que temos itens suficientes para preencher os slots
        int itemsToPick = Mathf.Min(shopSlots.Count, availableItems.Count);

        for (int i = 0; i < itemsToPick; i++)
        {
            // Pega um item aleatório da lista de disponíveis
            int randomIndex = Random.Range(0, availableItems.Count);
            ShopItemData randomItem = availableItems[randomIndex];

            // Adiciona na lista de itens atuais e remove da lista de disponíveis para não repetir
            currentShopItems.Add(randomItem);
            availableItems.RemoveAt(randomIndex);

            // Configura o slot da UI correspondente com os dados do item sorteado
            shopSlots[i].Setup(randomItem, this);
        }
    }

    // Função de compra genérica!
    public void BuyItem(ShopItemData item)
    {
        if (CarteiraPlayer.Coin >= item.price)
        {
            CarteiraPlayer.Coin -= item.price;
            CarteiraPlayer.UpdateCoinText(); // Usando a função que criamos anteriormente

            // --- LÓGICA DE ADICIONAR O ITEM AO JOGADOR ---
            // Aqui é onde você implementaria um sistema de inventário.
            // Por enquanto, vamos apenas mostrar uma mensagem.
            Debug.Log($"Comprou {item.itemName} por {item.price} moedas!");
            // Exemplo: PlayerInventory.AddItem(item.itemID);

        }
        else
        {
            Debug.Log("Sem moedas suficientes!");
        }
    }
}