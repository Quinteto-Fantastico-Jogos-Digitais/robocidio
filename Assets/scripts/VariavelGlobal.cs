using UnityEngine;
using TMPro;

public class VariavelGlobal : MonoBehaviour
{
    public long pontos = 0;
    private float startTime;
    public float elapsed;
    public long killCount = 0;
    private int horda = 1;

    //perks
    public float velocidade = 10f;
    public bool QuickRevive = false;
    public bool Explosion = false;
    public int MuleQuick = 1; //quantidade de espadas
    public int HealthUpgrade = 0;
    public int Luck = 0;

    public GameObject GUI;

    private int indicePontos = 0;
    private int indiceHorda = 1;
    private int indiceTempo = 2;

    private TMP_Text pontosContagem;
    private TMP_Text hordaContagem;
    private TMP_Text tempoContagem;
    private GameObject GameOver;
    private TMP_Text gameOverAux;
    private GameObject Blood;
    private TextMeshProUGUI Aux;

    public bool glorpCooldown = false;

    public GameObject LOJA;
    private LojaOn lolja;
    private TMP_Text pontosLoja;
    private TMP_Text vidaLoja;
    private TMP_Text upgradeVidaTexto;
    private TMP_Text upgradeVidaCusto;
    private TMP_Text apostaQuantidade;

    public int upgradeVidaCustoNumerico = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startTime = Time.time;  // marca início da fase

        pontosContagem = GUI.transform.GetChild(indicePontos).GetChild(0).GetComponent<TMP_Text>();
        hordaContagem = GUI.transform.GetChild(indiceHorda).GetChild(0).GetComponent<TMP_Text>();
        tempoContagem = GUI.transform.GetChild(indiceTempo).GetChild(0).GetComponent<TMP_Text>();
        GameOver = GUI.transform.GetChild(3).gameObject;
        gameOverAux = GUI.transform.GetChild(3).GetChild(1).GetComponent<TMP_Text>();
        Blood = GUI.transform.GetChild(4).gameObject;
        Aux = GUI.transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>();

        lolja = LOJA.GetComponent<LojaOn>();
        pontosLoja = LOJA.transform.GetChild(0).GetChild(0).GetChild(3).GetChild(0).GetComponent<TMP_Text>();
        vidaLoja = LOJA.transform.GetChild(0).GetChild(0).GetChild(2).GetChild(0).GetComponent<TMP_Text>();
        upgradeVidaTexto = LOJA.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<TMP_Text>();
        upgradeVidaCusto = LOJA.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetComponent<TMP_Text>();
        upgradeVidaCustoNumerico = int.Parse(upgradeVidaCusto.text);

        apostaQuantidade = LOJA.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(2).GetChild(0).GetComponent<TMP_Text>();

        pontosContagem.SetText("{0}", 0);
        hordaContagem.SetText("{0}", 1f);
    }

    // Update is called once per frame
    void Update()
    {
        elapsed = Time.time - startTime;

        int total = Mathf.FloorToInt(elapsed);
        int horas = total / 3600;
        int minutos = (total % 3600) / 60;
        int segundos = total % 60;

        tempoContagem.SetText("{0:00}:{1:00}:{2:00}", horas, minutos, segundos);
    }

    public void SomaKills()
    {
        killCount += 1;
    }

    public void SomaHorda()
    {
        horda += 1;
        hordaContagem.SetText("{0}", horda);
    }

    public void SomaPontos(long qtd)
    {
        pontos += qtd;
        pontosContagem.SetText("{0}", pontos);
        pontosLoja.SetText("{0}", pontos);
    }

    public void SubtraiPontos(long qtd)
    {
        pontos -= qtd;
        pontosContagem.SetText("{0}", pontos);
        pontosLoja.SetText("{0}", pontos);
    }

    public void setVida(float qtd)
    {
        vidaLoja.SetText("{0}", qtd);
    }

    public void setUpgradeText(string text)
    {
        upgradeVidaTexto.SetText(text);
    }

    public void setUpgradeCusto(string text)
    {
        upgradeVidaCusto.SetText(text);
    }

    public void setApostaQuantidade(long pontos)
    {
        apostaQuantidade.SetText("{0}", pontos);
    }

    public void StartTomouDano()
    {
        Blood.SetActive(true);
        Invoke(nameof(EndTomouDano), 0.5f);
    }

    public void EndTomouDano()
    {
        Blood.SetActive(false);
    }

    public void setTextoAux(string texto)
    {
        Aux.SetText(texto);
    }

    public void abreLoja()
    {
        LOJA.SetActive(true);
        lolja.lojaOn();
    }

    public void fechaLoja()
    {
        LOJA.SetActive(false);
    }

    public void CallGameOver()
    {
        GameOver.SetActive(true);

        int tipoControle = PlayerPrefs.GetInt("tipoControle", 0);

        if (tipoControle == 0)
        {
            gameOverAux.SetText("--- Pressione a tecla <color=#FFFF00>R</color> para recomecar ---");
        }
        else
        {
            gameOverAux.SetText("--- Pressione a tecla <color=#FFFF00>X</color> para recomecar ---");
        }

        Time.timeScale = 0f;   // pausa TUDO
        Time.fixedDeltaTime = 0f; // pausa física corretamente
    }
}
