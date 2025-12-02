using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenuFuncional : MonoBehaviour
{
    [Header("Configuração Geral")]
    [Tooltip("Arraste aqui o objeto 'Canvas' ou o Pai de todos que deve sumir ao clicar em Jogar")]
    public GameObject objetoDoMenuCompleto;

    [Header("Painéis Internos")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;

    private void OnEnable()
    {
        // Garante que resetamos os painéis ao abrir
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
        
        // Opcional: Se quiser garantir o pause aqui também
        // Time.timeScale = 0f; 
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
        // senão a próxima cena carrega pausada.
        Time.timeScale = 1f;

        Debug.Log("Saindo...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}