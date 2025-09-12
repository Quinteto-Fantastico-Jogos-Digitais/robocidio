using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class CoinCollection : MonoBehaviour
{
    private int Coin = 0;

    public TextMeshProUGUI coinText;

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Coin")
        {
            Coin++;
            coinText.text = "Coin: " + Coin.ToString();
            Debug.Log(Coin);
            Destroy(other.gameObject);
        }
    }
}
