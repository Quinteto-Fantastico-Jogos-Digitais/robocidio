using UnityEngine;
using UnityEngine.UI;

public class UIColorHexCycleSmooth : MonoBehaviour
{
    public Image img;
    public float durationPerSegment = 1f;
    private Color[] cores;
    private int i = 0;
    private float t = 0f;

    void Start()
    {
        if (img == null) img = GetComponent<Image>();

        cores = new Color[]
        {
            new Color32(255, 0,   0,   255), // Red
            new Color32(255, 0,   255, 255), // Magenta
            new Color32(0,   0,   255, 255), // Blue
            new Color32(0,   255, 255, 255), // Cyan
            new Color32(0,   255, 0,   255), // Green
            new Color32(255, 255, 0, 255),   // Yellow
            new Color32(255, 0,   0,   255), // Red (fecha o ciclo)
        };
    }

    void Update()
    {
        t += Time.deltaTime / durationPerSegment;

        if (t >= 1f)
        {
            t = 0f;
            i = (i + 1) % (cores.Length - 1); 
        }

        img.color = Color.Lerp(cores[i], cores[i + 1], t);
    }
}
