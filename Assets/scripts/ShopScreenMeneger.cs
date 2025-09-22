using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ShopScreenMenagemant : MonoBehaviour
{
    //Referenciais para os objetos
    public GameObject Arena;
    public GameObject Loja;

    public void DesligarTudo()
    {
        Arena.SetActive(false);
        Loja.SetActive(false);
    }

    public void MudarParaArena()
    {
        DesligarTudo();
        Arena.SetActive(true);
        Debug.Log("Botão clicado");
    }

    public void MudarParaLoja()
    {
        DesligarTudo();
        Loja.SetActive(true);
        Debug.Log("Botão clicado");
    }

}
