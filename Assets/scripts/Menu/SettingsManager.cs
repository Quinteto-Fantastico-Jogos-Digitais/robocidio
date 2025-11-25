using Unity.VisualScripting;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public float mouseSensitivity = 1f;
    public float masterVolume = 1f;
    public int TipoControle = 0; //0 para Mouse e Teclado. 1 para Joycon

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings(); // carrega quando o jogo inicia
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("mouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetFloat("masterVolume", masterVolume);
        PlayerPrefs.SetInt("tipoControle", TipoControle);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("mouseSensitivity", 1f);
        masterVolume = PlayerPrefs.GetFloat("masterVolume", 1f);
        TipoControle = PlayerPrefs.GetInt("tipoControle", 0);
    }

    public void setTipoControle(int index)
    {
        TipoControle = index;
        SaveSettings();
    }
}