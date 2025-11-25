using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ROLETA : MonoBehaviour
{
    [Header("Refs (arraste no Inspector)")]
    public RectTransform wheel;     // RectTransform da Image da roleta (pivot 0.5,0.5)
    public Button spinButton;       // botão que inicia o giro (opcional)
    public int segments = 2;        // número de fatias na roleta

    [Header("Tuning")]
    public int minFullRotations = 12;    // mínimo de voltas completas
    public int maxFullRotations = 18;    // máximo de voltas completas
    public float initialSpeed = 2200f;   // velocidade inicial (graus/s)
    public float decelFactorPerFrame = 0.993f; // multiplicador por frame (<1, quanto menor mais rápido para)
    public float minSpeed = 40f;         // velocidade de corte (quando chega aqui faz o ajuste final)

    public int winner = 0; //se 0 então ganha se 1 então perde

    // callback opcional
    public Action<int> OnSpinFinished;

    bool spinning = false;

    void Start()
    {
        if (spinButton != null)
            spinButton.onClick.AddListener(StartSpin);
    }

    public void StartSpin()
    {
        if (spinning) return;
        StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
    {
        spinning = true;
        if (spinButton != null) spinButton.interactable = false;

        // segurança básica
        if (wheel == null || segments <= 0)
        {
            Debug.LogWarning("ROLETA: wheel nula ou segments inválido.");
            spinning = false;
            if (spinButton != null) spinButton.interactable = true;
            yield break;
        }

        // decide alvo e quantas voltas
        int fullRot = UnityEngine.Random.Range(minFullRotations, maxFullRotations + 1);
        int targetSegment = UnityEngine.Random.Range(0, segments);

        float segAngle = 360f / segments;
        // offset dentro do segmento para NÃO ficar sempre centrado (parece mais natural)
        float offsetInside = UnityEngine.Random.Range(segAngle * 0.12f, segAngle * 0.88f);

        // total de graus que queremos girar (positivo)
        float totalRotation = fullRot * 360f + targetSegment * segAngle + offsetInside;

        // estado inicial
        float startZ = wheel.eulerAngles.z;
        float rotatedSoFar = 0f;
        float speed = initialSpeed;

        int safetyCounter = 0;
        int safetyMax = 200000; // evita infinito em caso estranho

        // gira até atingir a rotação desejada (acumulada)
        while (rotatedSoFar < totalRotation && safetyCounter < safetyMax)
        {
            float dt = Time.deltaTime;
            // delta que vamos girar neste frame (graus)
            float delta = speed * dt;

            // aplica rotação (negativo para girar sentido horário visual)
            wheel.Rotate(0f, 0f, -delta);

            rotatedSoFar += delta;

            // desacelera multiplicativamente (simula atrito)
            speed *= Mathf.Clamp(decelFactorPerFrame, 0.8f, 0.9999f);

            // garantia: não deixe speed ficar NaN ou infinito
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f)
                speed = minSpeed;

            // Se caiu abaixo do minSpeed e já passou de (totalRotation - uma margem), podemos sair
            if (speed < minSpeed)
            {
                // garante que em poucos frames a interp final vai corrigir preciso
                break;
            }

            safetyCounter++;
            yield return null;
        }

        // Faz correção final suave para o ângulo exato (evita pequenos erros acumulados)
        float finalZ = startZ - totalRotation; // o z final desejado
        float curZ = wheel.eulerAngles.z;
        float correctionDuration = 0.25f;
        float t = 0f;
        while (t < correctionDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / correctionDuration);
            float z = Mathf.LerpAngle(curZ, finalZ, k);
            wheel.rotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        wheel.rotation = Quaternion.Euler(0f, 0f, finalZ);

        // calcula vencedor usando o LADO DIREITO (3h) como marca
        //  - converte endedZ para 0..360
        float endedZ = wheel.eulerAngles.z % 360f;
        if (endedZ < 0f) endedZ += 360f;

        // ângulo no sentido horário a partir do topo (12h)
        float clockwiseFromTop = (360f - endedZ) % 360f;
        // desloca para a direita (3h) => subtrai 90°
        float clockwiseFromRight = (clockwiseFromTop - 90f + 360f) % 360f;

        winner = Mathf.FloorToInt(clockwiseFromRight / segAngle) % segments;
        if (winner < 0) winner += segments;

        Debug.Log($"ROLETA: parou. targetSegment={targetSegment}, winner={winner}");

        OnSpinFinished?.Invoke(winner);

        spinning = false;
        if (spinButton != null) spinButton.interactable = true;
    }
}
