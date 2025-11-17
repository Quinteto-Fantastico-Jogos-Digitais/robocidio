using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuOptions
{

    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    
    public void Start() {

    }

    public void AbrirOpcoes() {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes() {
        painelOpcoes.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void SairDoJogo() {
        Debug.Log("Sair do jogo");
        Application.Quit();
    }
}
