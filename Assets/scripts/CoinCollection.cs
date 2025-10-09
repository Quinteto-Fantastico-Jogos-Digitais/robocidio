using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class CoinCollection : MonoBehaviour
{
    public int Coin = 0;

    public TextMeshProUGUI coinText;
    void Start()
    {
        UpdateCoinText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Coin")
        {
            Coin++;
            UpdateCoinText();
            Debug.Log(Coin);
            Destroy(other.gameObject);
        }
          }

    public void UpdateCoinText()
    {
        if(coinText != null)
        {
            coinText.text = "Coin: " + Coin.ToString();
        }
    }
}