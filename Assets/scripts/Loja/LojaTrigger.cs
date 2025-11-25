using TMPro;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public Transform playerBody;       // optional: used to evaluate local-space clamps
    public VariavelGlobal variaveisGlobais;

    private bool colidindo = false;

    void Update()
    {
        if (colidindo)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                variaveisGlobais.abreLoja();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!this.enabled) return;

        Debug.Log("Colidi com: " + other.gameObject.name);

        //Se for inimigo chama a função de morrer
        if (other.gameObject.name == playerBody.gameObject.name)
        {
            variaveisGlobais.setTextoAux("Pressione <color=#FFFF00>E</color> abrir a Loja.");
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

    
}
