using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuOptions : MonoBehaviour
{
    
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    public TMP_Dropdown graficosDropdown;

    void Start()
    {
        if (painelMenuInicial != null) painelMenuInicial.SetActive(true);
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
    }

    public void AbrirOpcoes()
    {
        if (painelMenuInicial != null) painelMenuInicial.SetActive(false);
        if (painelOpcoes != null) painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
        if (painelMenuInicial != null) painelMenuInicial.SetActive(true);
    }

    public void SairDoJogo()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void Jogar(string nomeDaCenaPrincipal)
    {
        SceneManager.LoadScene(nomeDaCenaPrincipal);
    }
}