using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // <--- NECESSÁRIO PARA O SLIDER

public class EscMenuFuncional : MonoBehaviour
{
    [Header("Configuração Geral")]
    [Tooltip("Arraste aqui o objeto 'Canvas' ou o Pai de todos que deve sumir ao clicar em Jogar")]
    public GameObject objetoDoMenuCompleto;

    [Header("Painéis Internos")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;

    [Header("Configuração de Sensibilidade")]
    [Tooltip("Arraste o Slider de sensibilidade aqui")]
    public Slider sliderSensibilidade;
    
    [Tooltip("Arraste o Objeto do Player (que contém os scripts de movimento) aqui")]
    public GameObject playerObject;

    // Valores base definidos nos seus scripts originais
    private float baseMouseSens = 8.0f;
    private float baseJoyconSens = 1.6f;

    // Referências aos scripts do player
    private MainCharacterController scriptMouse;
    private MainCharacterControllerJoyCon scriptJoycon;

    private void Start()
    {
        // 1. Busca os scripts no Player
        if (playerObject != null)
        {
            scriptMouse = playerObject.GetComponent<MainCharacterController>();
            scriptJoycon = playerObject.GetComponent<MainCharacterControllerJoyCon>();
        }

        // 2. Carrega valor salvo ou usa 1.0
        float multiplicadorSalvo = PlayerPrefs.GetFloat("SensibilidadeMultiplicador", 1.0f);

        // 3. Configura o slider visualmente e aplica no jogo
        if (sliderSensibilidade != null)
        {
            sliderSensibilidade.value = multiplicadorSalvo;
            sliderSensibilidade.onValueChanged.AddListener(AtualizarSensibilidade);
        }

        // 4. Força a atualização inicial para garantir que o player comece com a sensibilidade certa
        AtualizarSensibilidade(multiplicadorSalvo);
    }

    private void OnEnable()
    {
        // Garante que resetamos os painéis ao abrir
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
        
        // Se quiser garantir que o slider esteja visualmente correto ao reabrir o menu
        if (sliderSensibilidade != null)
        {
            sliderSensibilidade.value = PlayerPrefs.GetFloat("SensibilidadeMultiplicador", 1.0f);
        }
    }

    // Função chamada dinamicamente pelo Slider
    public void AtualizarSensibilidade(float multiplicador)
    {
        // Aplica no Script de Mouse (Base 8 * multiplicador)
        if (scriptMouse != null)
        {
            scriptMouse.mouseYawSensitivity = baseMouseSens * multiplicador;
            scriptMouse.mousePitchSensitivity = baseMouseSens * multiplicador;
        }

        // Aplica no Script de Joycon (Base 1.6 * multiplicador)
        if (scriptJoycon != null)
        {
            scriptJoycon.stickSensitivity = baseJoyconSens * multiplicador;
        }

        // Salva
        PlayerPrefs.SetFloat("SensibilidadeMultiplicador", multiplicador);
    }

    public void ContinuarJogo()
    {
        Debug.Log("Botão Continuar clicado!");

        // 1. Despausa o jogo ANTES de fechar
        Time.timeScale = 1f;

        // 2. Fecha o objeto
        if (objetoDoMenuCompleto != null)
        {
            objetoDoMenuCompleto.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 3. Destrava mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void AbrirOpcoes()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(false);
        if (painelOpcoes != null) painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
    }

    public void SairDoJogo()
    {
        // IMPORTANTE: Sempre volte o tempo ao normal antes de sair da cena
        Time.timeScale = 1f;

        Debug.Log("Saindo...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}