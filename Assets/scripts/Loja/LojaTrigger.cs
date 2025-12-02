using TMPro;
using UnityEngine;

public class LojaTrigger : MonoBehaviour
{
    public Transform playerBody;       // optional: used to evaluate local-space clamps
    public VariavelGlobal variaveisGlobais;

    private bool colidindo = false;
    public bool SwitchClicou = false;

    void Update()
    {
        if (colidindo)
        {
            if (Input.GetKeyDown(KeyCode.E) || SwitchClicou)
            {
                variaveisGlobais.abreLoja();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!this.enabled) return;

        int tipoControle = PlayerPrefs.GetInt("tipoControle", 0);

        Debug.Log("Colidi com: " + other.gameObject.name);
        if (other.gameObject.name == playerBody.gameObject.name)
        {
            if (tipoControle == 0)
            {
                variaveisGlobais.setTextoAux("Pressione <color=#FFFF00>E</color> abrir a Loja.");
                
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

    
}
