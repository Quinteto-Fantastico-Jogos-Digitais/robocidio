using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// PlayerMovementRefactor (ajustado: stick sensitivity + response curve + faster defaults)
/// - Player (cylinder) gira no yaw (eixo Y) com stick de look ou mouse
/// - Câmera filha gira apenas no pitch (eixo X)
/// - Movement twin-stick no plano horizontal relativo ao player (ou teclado WASD)
/// - Parâmetros para ajustar sensibilidade e curva de resposta do stick
/// - Mouse + teclado adicionados com o mínimo de alterações; aplicação de rotação em LateUpdate para evitar jitter
/// </summary>
public class PlayerMovement1 : MonoBehaviour
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

    [Header("Keyboard & Mouse")]
    public bool enableKeyboard = true;
    public bool enableMouse = true;
    [Tooltip("Multiplica o delta do mouse antes de aplicar camera*Speed (1 = padrão).")]
    public float mouseSensitivity = 1f;

    // internos
    private List<Joycon> joycons;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velRef = Vector3.zero;

    private float currentYawVel = 0f;
    private float currentPitchVel = 0f;

    private float pitch = 0f;
    // rotações suavizadas aplicadas em LateUpdate para evitar jitter
    private float yawAngle = 0f;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // fallback para Camera.main se não setado
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // inicializa pitch com rotação local atual da câmera (normalizada)
        if (cameraTransform != null)
        {
            pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // inicializa yawAngle com a rotação atual do player
        yawAngle = NormalizeAngle(transform.eulerAngles.y);
    }

    void Update()
    {
        float camX = 0f, camY = 0f;
        float moveX = 0f, moveY = 0f;

        // --- leitura de joycons (se existirem) ---
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

        //Volta o mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        

        // --- keyboard (WASD) input tem prioridade se habilitado e existir input ---
        if (enableKeyboard)
        {
            float kx = Input.GetAxis("Horizontal"); // A/D ou left/right
            float ky = Input.GetAxis("Vertical");   // W/S ou up/down
            // considerar deadzone mínima
            if (Mathf.Abs(kx) > deadZone || Mathf.Abs(ky) > deadZone)
            {
                moveX = kx;
                moveY = ky;
            }
        }

        // --- mouse input tem prioridade para look se habilitado e existir movimento do mouse ---
        /*bool usingMouseThisFrame = false;
        if (enableMouse)
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            if (Mathf.Abs(mx) > 0f || Mathf.Abs(my) > 0f)
            {
                camX = mx * mouseSensitivity;
                camY = my * mouseSensitivity;
                usingMouseThisFrame = true;
            }
        }*/
        // --- mouse input tem prioridade para look se habilitado e existir movimento do mouse ---
        // IMPORTANTE: se o JoyconSwordController está capturando o mouse para mover a espada (botão direito),
        // o PlayerMovement NÃO deve usar o mouse para mover a câmera — essa checagem é mínima e segura.
        bool usingMouseThisFrame = false;
        if (enableMouse && !JoyconSwordController1.IsMouseCapturingSword)
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            if (Mathf.Abs(mx) > 0f || Mathf.Abs(my) > 0f)
            {
                camX = mx * mouseSensitivity;
                camY = my * mouseSensitivity;
                usingMouseThisFrame = true;
            }
        }

        // deadzone circular (para sticks)
        if (!usingMouseThisFrame) // mouse não usa deadzone
        {
            Vector2 camStick = new Vector2(camX, camY);
            if (camStick.magnitude < deadZone) { camX = camY = 0f; }

            Vector2 moveStick = new Vector2(moveX, moveY);
            if (moveStick.magnitude < deadZone) { moveX = moveY = 0f; }
        }

        // aplica sensibilidade e curva ao stick de look apenas se NÃO estamos usando mouse
        if (!usingMouseThisFrame)
        {
            camX = ApplyStickResponse(camX);
            camY = ApplyStickResponse(camY);
        }

        float dt = Time.deltaTime;

        // --- Look ---
        // yaw: atualizamos yawAngle internamente (aplicação real no LateUpdate)
        float yawDelta = camX * cameraYawSpeed * dt;
        float targetYaw = NormalizeAngle(yawAngle + yawDelta);
        float smoothYaw;

        if (lookSmoothTime <= 0f)
            yawAngle = targetYaw;
        else
        {
            //yawAngle = Mathf.SmoothDampAngle(yawAngle, targetYaw, ref currentYawVel, lookSmoothTime);
            smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref currentYawVel, lookSmoothTime);
            yawAngle = smoothYaw;
        }
            
        // pitch na câmera filha (aplicando invertY e limites)
        float effectiveCamY = invertY ? -camY : camY;
        float targetPitch = pitch - effectiveCamY * cameraPitchSpeed * dt;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        if (lookSmoothTime <= 0f)
            pitch = targetPitch;
        else
            pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref currentPitchVel, lookSmoothTime);

        // --- Movement (player-relative) ---
        Vector3 desiredVelocity = (transform.forward * moveY + transform.right * moveX) * moveSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);
        transform.position += currentVelocity * dt;
    }

    // aplicar rotações no LateUpdate reduz jitter quando a câmera está sendo atualizada mais rápido que a física/player
    void LateUpdate()
    {
        // aplica yaw ao player (mantemos X/Z inalterados)
        Vector3 playerEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(playerEuler.x, yawAngle, playerEuler.z);

        // aplica pitch na câmera filha
        if (cameraTransform != null)
        {
            Vector3 camLocalEuler = cameraTransform.localEulerAngles;
            camLocalEuler.x = pitch;
            cameraTransform.localEulerAngles = camLocalEuler;
        }
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
