using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // <--- NECESSÁRIO PARA O SLIDER
using TMPro;

public class MainMenuOptions : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    
    [Header("Configurações")]
    public TMP_Dropdown graficosDropdown;
    [Tooltip("Arraste o Slider de sensibilidade aqui")]
    public Slider sliderSensibilidade;

    void Start()
    {
        if (painelMenuInicial != null) painelMenuInicial.SetActive(true);
        if (painelOpcoes != null) painelOpcoes.SetActive(false);

        // --- CARREGAR SENSIBILIDADE SALVA ---
        if (sliderSensibilidade != null)
        {
            // Carrega o valor salvo ou usa 1.0 (padrão)
            float multiplicadorSalvo = PlayerPrefs.GetFloat("SensibilidadeMultiplicador", 1.0f);
            sliderSensibilidade.value = multiplicadorSalvo;

            // Adiciona o evento para salvar quando mover o slider
            sliderSensibilidade.onValueChanged.AddListener(AtualizarSensibilidade);
        }
    }

    // Chamado pelo Slider
    public void AtualizarSensibilidade(float valor)
    {
        // No Menu Principal, apenas salvamos a preferência
        PlayerPrefs.SetFloat("SensibilidadeMultiplicador", valor);
        PlayerPrefs.Save();
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