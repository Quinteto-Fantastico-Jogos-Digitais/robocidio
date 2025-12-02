using UnityEngine;
using System.Collections;

public class SomController : MonoBehaviour
{
    public AudioSource SonsZombies;    // arraste o AudioSource no inspector
    public AudioClip[] grunts;         // coleçao de grunhidos

    public AudioSource MusicaPrincipal;
    public AudioClip[] Musicas;

    public AudioSource SonsCasa;
    public AudioClip[] Casa;

    public AudioSource SonsEspada;
    public AudioClip[] Espada;

    public int Horda = 0;

    // flags para evitar múltiplos loops/coroutines simultâneos
    private bool musicaTocando = false;
    private bool randomLoopRunning = false;

    // contador para debug (quantas coroutines ativas de áudio)
    private int activeAudioCoroutines = 0;

    // --------------------
    // UTIL: debug rápido para saber se coroutines estão explodindo
    // pressione F1 na cena para logar (remova em release)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("Active audio coroutines: " + activeAudioCoroutines);
        }
    }

    // --------------------
    // SONS (SFX) — usa PlayOneShot para permitir sobreposição
    public void PlayRandomGruntAndWait()
    {
        if (grunts == null || grunts.Length == 0 || SonsZombies == null) return;
        AudioClip clip = grunts[Random.Range(0, grunts.Length)];
        StartCoroutine(PlayAndWaitCoroutineOneShot(SonsZombies, clip));
    }

    public void PlayRandomCasaAndWait()
    {
        if (Casa == null || Casa.Length == 0 || SonsCasa == null) return;
        AudioClip clip = Casa[Random.Range(0, Casa.Length)];
        StartCoroutine(PlayAndWaitCoroutineOneShot(SonsCasa, clip));
    }

    public void PlayEspadaAndWait(int index)
    {
        if (Espada == null || index < 0 || index >= Espada.Length || SonsEspada == null) return;
        AudioClip clip = Espada[index];
        StartCoroutine(PlayAndWaitCoroutineOneShot(SonsEspada, clip));
    }

    // Um coroutine genérico que PlayOneShot e espera o clip terminar
    IEnumerator PlayAndWaitCoroutineOneShot(AudioSource source, AudioClip clip)
    {
        if (clip == null || source == null) yield break;

        activeAudioCoroutines++;
        source.PlayOneShot(clip);
        // esperar o tempo do clip (menos uma pequena margem se quiser)
        yield return new WaitForSeconds(clip.length);
        activeAudioCoroutines--;
    }

    // --------------------
    // MÚSICA PRINCIPAL (loop com flag)
    public void PlayMusicaPrincipalAndWait()
    {
        // evita iniciar novamente se já estiver rodando
        if (musicaTocando) return;
        if (MusicaPrincipal == null || Musicas == null || Musicas.Length == 0) return;

        AudioClip clip = Musicas[0];
        // checar limites maiores primeiro se quiser outra lógica
        if (Horda >= 25 && Musicas.Length > 2)
        {
            clip = Musicas[2];
        }
        else if (Horda >= 15 && Musicas.Length > 1)
        {
            clip = Musicas[1];
        }

        StartCoroutine(PlayAndWaitCoroutineMusicaPrincipal(clip));
    }

    IEnumerator PlayAndWaitCoroutineMusicaPrincipal(AudioClip clip)
    {
        if (clip == null || MusicaPrincipal == null) yield break;

        musicaTocando = true;
        MusicaPrincipal.clip = clip;
        MusicaPrincipal.Play();

        // espera terminar
        yield return new WaitWhile(() => MusicaPrincipal.isPlaying);

        musicaTocando = false;

        // cancela invokes anteriores e agenda única chamada daqui a 8s
        CancelInvoke(nameof(PlayMusicaPrincipalAndWait));
        Invoke(nameof(PlayMusicaPrincipalAndWait), 8f);
    }

    // CallGameOver: stop everything e tocar musica final (checando length)
    public void CallGameOver()
    {
        if (SonsCasa != null) SonsCasa.Stop();
        if (SonsZombies != null) SonsZombies.Stop();
        if (MusicaPrincipal != null) MusicaPrincipal.Stop();
        if (SonsEspada != null) SonsEspada.Stop();

        // verificar se existe Musicas[3]
        if (Musicas != null && Musicas.Length > 3 && MusicaPrincipal != null)
        {
            // tocar música final diretamente (sem iniciar múltiplas)
            StartCoroutine(PlayAndWaitCoroutineMusicaPrincipal(Musicas[3]));
        }
    }

    // --------------------
    // Random sound loop (recomendado: coroutine única)
    public void StartRandomSoundLoop(float intervalBetweenChecks = 5f)
    {
        if (randomLoopRunning) return;
        randomLoopRunning = true;
        StartCoroutine(RandomSoundLoopCoroutine(intervalBetweenChecks));
    }

    public void StopRandomSoundLoop()
    {
        randomLoopRunning = false;
        CancelInvoke(nameof(RandomSoundAndWait)); // só por segurança se você usa invoke noutro lugar
    }

    private IEnumerator RandomSoundLoopCoroutine(float intervalBetweenChecks)
    {
        // se não há clips, encerra
        if ((grunts == null || grunts.Length == 0) && (Casa == null || Casa.Length == 0))
        {
            randomLoopRunning = false;
            yield break;
        }

        while (randomLoopRunning)
        {
            int roll = Random.Range(0, 20);

            if (roll == 0)
            {
                if (Casa != null && Casa.Length > 0 && SonsCasa != null)
                {
                    AudioClip clipCasa = Casa[Random.Range(0, Casa.Length)];
                    // usamos PlayOneShot e esperamos o tempo do clip
                    yield return StartCoroutine(PlayAndWaitCoroutineOneShot(SonsCasa, clipCasa));
                }
            }
            else if (roll < 5)
            {
                if (grunts != null && grunts.Length > 0 && SonsZombies != null)
                {
                    AudioClip clipGrunt = grunts[Random.Range(0, grunts.Length)];
                    yield return StartCoroutine(PlayAndWaitCoroutineOneShot(SonsZombies, clipGrunt));
                }
            }

            // intervalo antes do próximo sorteio
            yield return new WaitForSeconds(intervalBetweenChecks);
        }
    }

    // Mantive o método original caso você queira usar invoke em vez do loop
    public void RandomSoundAndWait()
    {
        // não recomendado sem proteção; mantive só por compatibilidade
        // chame CancelInvoke antes para evitar empilhamento
        CancelInvoke(nameof(RandomSoundAndWait));
        // sorteio
        int roll = Random.Range(0, 20);

        if (roll == 0)
        {
            if (Casa != null && Casa.Length > 0 && SonsCasa != null && !SonsCasa.isPlaying)
            {
                AudioClip clipCasa = Casa[Random.Range(0, Casa.Length)];
                StartCoroutine(PlayAndWaitCoroutineOneShot(SonsCasa, clipCasa));
            }
        }
        else if (roll < 5)
        {
            if (grunts != null && grunts.Length > 0 && SonsZombies != null && !SonsZombies.isPlaying)
            {
                AudioClip clipGrunt = grunts[Random.Range(0, grunts.Length)];
                StartCoroutine(PlayAndWaitCoroutineOneShot(SonsZombies, clipGrunt));
            }
        }

        Invoke(nameof(RandomSoundAndWait), 2f);
    }
}
