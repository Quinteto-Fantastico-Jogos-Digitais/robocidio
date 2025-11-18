using UnityEngine;
using TMPro;

public class VariavelGlobal : MonoBehaviour
{

    public long pontos = 0;
    private float startTime;
    public float elapsed;
    public long killCount = 0;
    public int horda = 1;

    public GameObject GUI;

    private int indicePontos = 0;
    private int indiceHorda = 1;
    private int indiceTempo = 2;

    private TMP_Text pontosContagem;
    private TMP_Text hordaContagem;
    private TMP_Text tempoContagem;
    private GameObject GameOver;
    private GameObject Blood;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startTime = Time.time;  // marca início da fase

        pontosContagem = GUI.transform.GetChild(indicePontos).GetChild(0).GetComponent<TMP_Text>();
        hordaContagem = GUI.transform.GetChild(indiceHorda).GetChild(0).GetComponent<TMP_Text>();
        tempoContagem = GUI.transform.GetChild(indiceTempo).GetChild(0).GetComponent<TMP_Text>();
        GameOver = GUI.transform.GetChild(3).gameObject;
        Blood = GUI.transform.GetChild(4).gameObject;

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

    public void SomaPontos(int qtd)
    {
        pontos += qtd;
        AtualizaTexto(indicePontos, pontos);
    }

    public void AtualizaTexto(int indice, int qtd)
    {
        Debug.Log("cheguei pra somar int");
        if (indice == indiceHorda)
        {
            hordaContagem.SetText("{0}", qtd);
        } 
    }

    public void AtualizaTexto(int indice, long qtd)
    {
        if (indice == 0) {
            pontosContagem.SetText("{0}", qtd);
        }
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

    public void CallGameOver()
    {
        GameOver.SetActive(true);
        Time.timeScale = 0f;   // pausa TUDO
        Time.fixedDeltaTime = 0f; // pausa física corretamente
    }
}
