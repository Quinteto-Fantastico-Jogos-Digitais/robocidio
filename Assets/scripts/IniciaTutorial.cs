using UnityEngine;

public class IniciaTutorial : MonoBehaviour
{
    private int tipoControle = 0;
    public GameObject Joycon;
    public GameObject Mouse;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tipoControle = PlayerPrefs.GetInt("tipoControle", 0);
        if (tipoControle == 0)
        {
            Mouse.SetActive(true);
        }
        else
        {
            Joycon.SetActive(true);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
