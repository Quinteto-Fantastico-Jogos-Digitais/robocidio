using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;

    private ShopItemData currentItem;
    private ShopMechanichals shopManager; // Referência ao script principal da loja

    // Método para configurar este slot com os dados de um item
    public void Setup(ShopItemData itemData, ShopMechanichals manager)
    {
        currentItem = itemData;
        shopManager = manager;

        nameText.text = currentItem.itemName;
        priceText.text = currentItem.price.ToString();
        iconImage.sprite = currentItem.itemIcon;

        // Adiciona um listener ao botão para chamar a função de compra
        buyButton.onClick.AddListener(OnBuyButtonClick);
    }

    private void OnBuyButtonClick()
    {
        // Avisa ao gerenciador da loja que o jogador quer comprar este item
        shopManager.BuyItem(currentItem);
    }
}