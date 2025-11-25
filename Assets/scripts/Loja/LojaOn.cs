using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class LojaOn : MonoBehaviour
{

    public VariavelGlobal variaveisGlobais;
    public Health health;

    public NavMeshSurface floor;
    public GameObject Player;
    public ROLETA roleta;

    private long apostaQuantidade = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        return;
    }

    public void lojaOn()
    {
        variaveisGlobais.setVida(health.CurrentHealth);
        variaveisGlobais.setApostaQuantidade(0);

        roleta.OnSpinFinished += rodaRoleta;

        if (health.CurrentHealth == health.maxHealth)
        {
            //seta o nome como upgrade
            variaveisGlobais.setUpgradeText("UPGRADE");
            variaveisGlobais.setUpgradeCusto("5000");
            variaveisGlobais.upgradeVidaCustoNumerico = 5000;
        }
        else
        {
            //seta o nome para cura
            variaveisGlobais.setUpgradeText("CURA");
            variaveisGlobais.setUpgradeCusto("500");
            variaveisGlobais.upgradeVidaCustoNumerico = 500;
        }

        //desativa o controle d player
        Player.GetComponent<MainCharacterController>().enabled = false;
        Player.GetComponent<MainCharacterControllerJoyCon>().enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        //desativa o chão (para os inimigos)
        floor.enabled = false;
    }

    public void lojaOff()
    {
        int tipoControle = PlayerPrefs.GetInt("tipoControle", 0);

        //Ativa o controle d player
        if (tipoControle == 0)
        {
            Player.GetComponent<MainCharacterController>().enabled = true;
        }
        else
        {
            Player.GetComponent<MainCharacterControllerJoyCon>().enabled = true;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //ativa o chão (volta os inimigos)
        floor.enabled = true;

        variaveisGlobais.fechaLoja();
    }

    public void CuraButton()
    {
        if (variaveisGlobais.pontos < variaveisGlobais.upgradeVidaCustoNumerico) return;
        variaveisGlobais.SubtraiPontos(variaveisGlobais.upgradeVidaCustoNumerico);

        if (health.CurrentHealth == health.maxHealth)
        {
            //seta o nome como upgrade
            health.Upgrade();
        }
        else
        {
            //seta o nome para cura
            health.Heal(999);
            variaveisGlobais.setUpgradeText("UPGRADE");
            variaveisGlobais.setUpgradeCusto("5000");
            variaveisGlobais.upgradeVidaCustoNumerico = 5000;
        }

        variaveisGlobais.setVida(health.CurrentHealth);
    }

    public void AllInButton()
    {
        apostaQuantidade = variaveisGlobais.pontos;
        variaveisGlobais.setApostaQuantidade(apostaQuantidade);
    }

    public void LowRiskButton()
    {
        if (apostaQuantidade >= variaveisGlobais.pontos) return;

        apostaQuantidade += 100;
        variaveisGlobais.setApostaQuantidade(apostaQuantidade);
    }

    public void MediumRiskButton()
    {
        if (apostaQuantidade >= variaveisGlobais.pontos) return;

        apostaQuantidade += 1000;
        variaveisGlobais.setApostaQuantidade(apostaQuantidade);
    }

    public void rodaRoleta(int winner)
    {

        if (winner == 0)
        {
            Debug.Log("GANHOU!");
            variaveisGlobais.SomaPontos(apostaQuantidade);
        }
        else
        {
            Debug.Log("PERDEU!");
            variaveisGlobais.SubtraiPontos(apostaQuantidade);
        }
            
        apostaQuantidade = 0;
        variaveisGlobais.setApostaQuantidade(apostaQuantidade);
    }
}
