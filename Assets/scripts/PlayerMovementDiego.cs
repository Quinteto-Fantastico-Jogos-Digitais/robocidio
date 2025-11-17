using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
// Assume-se que o enum ControlScheme está definido em um arquivo global separado.

/// <summary>
/// PlayerMovementRefactor (ajustado: stick sensitivity + response curve + faster defaults)
/// - Implementa twin-stick movement (Joycon) ou WASD/Mouse com Hot-Swapping.
/// - Câmera é FIXA neste script (a rotação é delegada à espada).
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // Variável para a troca automática (Hot-Swapping)
    private float lastSwitchTime = 0f;
    private const float INPUT_CHECK_WINDOW = 0.5f; 
    
    [Header("Control Scheme")]
    [Tooltip("Define qual esquema de controle está ativo (Joycon ou WASD).")]
    public ControlScheme activeControl = ControlScheme.Joycon;

    [Header("Joycon indices")]
    public int camJcIndex = 1;     // Stick para Look (Usado apenas para leitura no Hot-Swap)
    public int playerJcIndex = 0;  // Stick para Movimento (Usado para input)

    [Header("Refs")]
    public Transform cameraTransform; // câmera filha (se vazio tenta Camera.main)

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float moveSmoothTime = 0.08f;

    [Header("Mouse Look Sensitivity")]
    [Tooltip("Multiplicador da sensibilidade horizontal (Mouse X).")]
    [Range(0.1f, 50f)]
    public float mouseYawSensitivity = 10.0f; 

    [Tooltip("Multiplicador da sensibilidade vertical (Mouse Y).")]
    [Range(0.1f, 50f)]
    public float mousePitchSensitivity = 10.0f;

    [Header("Look (tune these)")]
    public float cameraYawSpeed = 360f; 
    public float cameraPitchSpeed = 240f;
    [Tooltip("Tempo de suavização para o look. 0 = sem suavização (imediato).")]
    public float lookSmoothTime = 0.02f;
    public float minPitch = -45f;
    public float maxPitch = 60f;
    public bool invertY = false;

    [Header("Stick tuning")]
    [Tooltip("Multiplicador simples para a magnitude do stick (1 = default, >1 = mais sensível).")]
    [Range(0.1f, 5f)]
    public float stickSensitivity = 1.6f; 

    [Tooltip("Expoente aplicado à entrada do stick: <1 deixa o stick MAIS sensível perto do centro; >1 deixa MAIS fino no centro.")]
    [Range(0.2f, 2f)]
    public float stickResponseExponent = 0.85f; 

    [Header("Input")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.08f; 

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

        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogError("PlayerMovement: Camera Transform não foi definido no Inspector e Camera.main não foi encontrada!");
        }

        pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    public void SetControlScheme(ControlScheme newScheme)
    {
        activeControl = newScheme;
    }

    void Update()
    {
        float camX = 0f, camY = 0f;
        float moveX = 0f, moveY = 0f;

        // --- 1. DETECÇÃO E TROCA DE ESQUEMA (HOT-SWAP) ---
        
        bool joyconInputDetected = false;
        Vector2 joyconCamStick = Vector2.zero;
        Vector2 joyconMoveStick = Vector2.zero;
        Joycon jMove = null;
        
        // Tenta ler Joycon sticks e botões para detecção
        if (joycons != null)
        {
            // Lendo Stick 1
            if (joycons.Count > camJcIndex && joycons[camJcIndex] != null)
            {
                float[] camStickArray = joycons[camJcIndex].GetStick();
                if (camStickArray != null && camStickArray.Length >= 2)
                    joyconCamStick = new Vector2(camStickArray[0], camStickArray[1]);
            }
            
            // Lendo Stick 0 (Movimento)
            if (joycons.Count > playerJcIndex && joycons[playerJcIndex] != null)
            {
                jMove = joycons[playerJcIndex];
                float[] moveStickArray = jMove.GetStick();
                if (moveStickArray != null && moveStickArray.Length >= 2)
                    joyconMoveStick = new Vector2(moveStickArray[0], moveStickArray[1]);
            }
        }
        
        // Verifica botões para Hot-Swapping (usando Stick 0/jMove)
        bool joyconButtonInput = jMove != null && (
            jMove.GetButton(Joycon.Button.DPAD_UP) || 
            jMove.GetButton(Joycon.Button.SHOULDER_2) || 
            jMove.GetButton(Joycon.Button.PLUS)); 
            
        // Verifica se houve movimento significativo nos sticks
        if (joyconCamStick.magnitude > 0.05f || joyconMoveStick.magnitude > 0.05f || joyconButtonInput) 
            joyconInputDetected = true;


        // Verifica Keyboard/Mouse Input
        bool keyboardMouseInputDetected = 
            Input.GetAxis("Mouse X") != 0 || 
            Input.GetAxis("Mouse Y") != 0 ||
            Input.GetButton("Horizontal") || 
            Input.GetButton("Vertical"); 
            
        // Aplica a Troca (com atraso)
        if (Time.time > lastSwitchTime + INPUT_CHECK_WINDOW)
        {
            if (joyconInputDetected && activeControl != ControlScheme.Joycon)
            {
                activeControl = ControlScheme.Joycon;
                lastSwitchTime = Time.time;
            }
            else if (keyboardMouseInputDetected && activeControl != ControlScheme.WASD)
            {
                activeControl = ControlScheme.WASD;
                lastSwitchTime = Time.time;
            }
        }


        // --- 2. LEITURA DE INPUT FINAL BASEADA NO ESQUEMA ATIVO ---
        
        if (activeControl == ControlScheme.Joycon)
        {
            // CÂMERA/LOOK: ZERADO (TRAVADO)
            camX = 0f;
            camY = 0f;
            
            // MOVIMENTO: Recebe os dados do STICK 0 (joyconMoveStick)
            moveX = joyconMoveStick.x;
            moveY = joyconMoveStick.y;

            // deadzone circular CORRIGIDO
            if (joyconMoveStick.magnitude < deadZone) 
            { 
                moveX = 0f; 
                moveY = 0f; 
            }

            // Aplica a curva de resposta (sem efeito em camX/Y = 0)
            camX = ApplyStickResponse(camX);
            camY = ApplyStickResponse(camY);
        }
        else // activeControl == ControlScheme.WASD
        {
            // --- Lógica de Input Teclado (WASD/Mouse) ---
            
            moveX = Input.GetAxisRaw("Horizontal"); 
            moveY = Input.GetAxisRaw("Vertical");   
            
            // CÂMERA: ZERADA (Mouse Look transferido para JoyconSwordController)
            camX = Input.GetAxis("Mouse X") * mouseYawSensitivity; 
            camY = Input.GetAxis("Mouse Y") * mousePitchSensitivity;
        }

        float dt = Time.deltaTime;

        // --- Look (Comum a ambos os esquemas) ---
        // O camX/Y (que é 0) é usado aqui para garantir que a rotação Yaw e Pitch seja zero.
        
        float yawDelta = camX * cameraYawSpeed * dt;
        float targetYaw = NormalizeAngle(transform.eulerAngles.y + yawDelta);

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

        // --- Movement (Comum a ambos os esquemas) ---
        Vector3 desiredVelocity = (transform.forward * moveY + transform.right * moveX) * moveSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);
        transform.position += currentVelocity * dt;
    }

    /// <summary>
    /// Aplica multiplicador e curva de resposta ao input do stick.
    /// </summary>
    private float ApplyStickResponse(float value)
    {
        float v = Mathf.Clamp(value * stickSensitivity, -1f, 1f);
        float sign = Mathf.Sign(v);
        float mag = Mathf.Abs(v);
        float adjusted = Mathf.Pow(mag, stickResponseExponent);
        return sign * adjusted;
    }

    private float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }
}