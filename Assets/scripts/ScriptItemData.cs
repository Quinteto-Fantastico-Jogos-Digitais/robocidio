using UnityEngine;


[CreateAssetMenu(fileName = "NovoItemLoja", menuName = "Sistema Loja/Item de Loja")]
public class ShopItemData : ScriptableObject
{
    [Header("Informações do Item")]
    public string itemName;
    public Sprite itemIcon;
    public int price;

    [Header("Detalhes para o Jogo")]
    public string itemID; 
}