using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerMovementRefactor (ajustado: stick sensitivity + response curve + faster defaults)
/// - Player (cylinder) gira no yaw (eixo Y) com stick de look
/// - Câmera filha gira apenas no pitch (eixo X)
/// - Movement twin-stick no plano horizontal relativo ao player
/// - Parâmetros para ajustar sensibilidade e curva de resposta do stick
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Joycon indices")]
    public int camJcIndex = 1;     // stick para look (yaw/pitch)
    public int playerJcIndex = 0;  // stick para mover o player

    [Header("Refs")]
    public Transform cameraTransform; // câmera filha (se vazio tenta Camera.main)

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float moveSmoothTime = 0.08f;

    [Header("Look (tune these)")]
    public float cameraYawSpeed = 360f;   // deg/s por unidade do stick horizontal (aumentado)
    public float cameraPitchSpeed = 240f; // deg/s por unidade do stick vertical (aumentado)
    [Tooltip("Tempo de suavização para o look. 0 = sem suavização (imediato).")]
    public float lookSmoothTime = 0.02f;  // reduzido para look mais responsivo
    public float minPitch = -45f;
    public float maxPitch = 60f;
    public bool invertY = false;

    [Header("Stick tuning")]
    [Tooltip("Multiplicador simples para a magnitude do stick (1 = default, >1 = mais sensível).")]
    [Range(0.1f, 5f)]
    public float stickSensitivity = 1.6f; // aumenta sensibilidade do stick

    [Tooltip("Expoente aplicado à entrada do stick: <1 deixa o stick MAIS sensível perto do centro; >1 deixa MAIS fino no centro.")]
    [Range(0.2f, 2f)]
    public float stickResponseExponent = 0.85f; // <1 => mais sensível no centro

    [Header("Input")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.08f; // ligeiramente menor que antes

    // internos
    private List<Joycon> joycons;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velRef = Vector3.zero;

    private float currentYawVel = 0f;
    private float currentPitchVel = 0f;

    private float pitch = 0f;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            // inicializa pitch com rotação local atual da câmera (normalizada)
            pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void Update()
    {
        float camX = 0f, camY = 0f;
        float moveX = 0f, moveY = 0f;

        if (joycons != null)
        {
            if (joycons.Count > camJcIndex && joycons[camJcIndex] != null)
            {
                var s = joycons[camJcIndex].GetStick();
                if (s != null && s.Length >= 2) { camX = s[0]; camY = s[1]; }
            }

            if (joycons.Count > playerJcIndex && joycons[playerJcIndex] != null)
            {
                var s = joycons[playerJcIndex].GetStick();
                if (s != null && s.Length >= 2) { moveX = s[0]; moveY = s[1]; }
            }
        }

        // deadzone circular (para sticks)
        Vector2 camStick = new Vector2(camX, camY);
        if (camStick.magnitude < deadZone) { camX = camY = 0f; }

        Vector2 moveStick = new Vector2(moveX, moveY);
        if (moveStick.magnitude < deadZone) { moveX = moveY = 0f; }

        // aplica sensibilidade e curva ao stick de look
        camX = ApplyStickResponse(camX);
        camY = ApplyStickResponse(camY);

        float dt = Time.deltaTime;

        // --- Look ---
        // yaw no player (rotaciona o corpo)
        float yawDelta = camX * cameraYawSpeed * dt;
        float targetYaw = NormalizeAngle(transform.eulerAngles.y + yawDelta);

        // Se lookSmoothTime <= 0 fazemos aplicação direta (imediato)
        float smoothYaw;
        if (lookSmoothTime <= 0f)
            smoothYaw = targetYaw;
        else
            smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref currentYawVel, lookSmoothTime);

        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, smoothYaw, transform.eulerAngles.z);

        // pitch na câmera filha (apenas X)
        float effectiveCamY = invertY ? -camY : camY;
        float targetPitch = pitch - effectiveCamY * cameraPitchSpeed * dt;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        if (lookSmoothTime <= 0f)
            pitch = targetPitch;
        else
            pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref currentPitchVel, lookSmoothTime);

        if (cameraTransform != null)
        {
            Vector3 camLocalEuler = cameraTransform.localEulerAngles;
            camLocalEuler.x = pitch;
            cameraTransform.localEulerAngles = camLocalEuler;
        }

        // --- Movement (player-relative) ---
        Vector3 desiredVelocity = (transform.forward * moveY + transform.right * moveX) * moveSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);
        transform.position += currentVelocity * dt;
    }

    /// <summary>
    /// Aplica multiplicador e curva de resposta ao input do stick.
    /// Exemplo: exponent < 1 -> mais sensível perto do centro.
    /// Mantém sinal do input.
    /// </summary>
    private float ApplyStickResponse(float value)
    {
        // aplica multiplicador primeiro
        float v = Mathf.Clamp(value * stickSensitivity, -1f, 1f);

        // aplica curva (preserva sinal)
        float sign = Mathf.Sign(v);
        float mag = Mathf.Abs(v);

        // se exponent == 1, fica linear
        float adjusted = Mathf.Pow(mag, stickResponseExponent);

        return sign * adjusted;
    }

    private float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }
}
