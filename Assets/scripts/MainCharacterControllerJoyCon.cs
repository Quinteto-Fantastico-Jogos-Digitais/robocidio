using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JoyconPlayerController_JoyconOnly
/// - Somente entradas via Joycon sticks (GetStick()).
/// - Sem mouse, sem keyboard.
/// - Botão '+' (PLUS) abre/fecha o Menu e Pausa o jogo.
/// - Botão 'A' (DPAD_RIGHT no Joycon R) pula.
/// </summary>
[DisallowMultipleComponent]
public class MainCharacterControllerJoyCon : MonoBehaviour
{
    [Header("Joycon indices")]
    public int playerJcIndex = 0;   // stick de movimento (Geralmente Left Joycon)
    public int camJcIndex = 1;      // stick de olhar (Geralmente Right Joycon)

    [Header("References")]
    public Transform cameraTransform; // atribua a câmera filha (ou deixe vazio para usar Camera.main)
    public Rigidbody characterRigidbody; // opcional: se presente, MovePosition será usado

    [Header("UI Overlay")]
    [Tooltip("Arraste aqui o GameObject (Canvas/Painel) que servirá de menu.")]
    public GameObject menuOverlay; 

    [Header("Movement")]
    public VariavelGlobal variaveisGlobais;
    public float moveSpeed = 10f;
    public float moveSmoothTime = 0.08f;

    [Header("Jump")]
    public float jumpForce = 5f;
    [Tooltip("Arraste o objeto vazio que fica nos pés do jogador")]
    public Transform groundCheckTarget;
    [Tooltip("Raio da esfera de detecção")]
    public float groundCheckRadius = 0.2f;
    [Tooltip("Selecione a Layer que o jogo deve considerar como Chão")]
    public LayerMask groundLayer;

    [Header("Look (Joycon stick)")]
    public float cameraYawSpeed = 360f;   // graus por segundo quando stick move X
    public float cameraPitchSpeed = 240f; // graus por segundo quando stick move Y
    public float stickSensitivity = 1.6f;
    public float stickResponseExponent = 0.85f;
    public float minPitch = -45f;
    public float maxPitch = 60f;

    [Header("Deadzone")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.08f;

    // internals
    private List<Joycon> joycons;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velRef = Vector3.zero;
    private float pitch = 0f;
    private float yawAngle = 0f;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();

        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            float camX = cameraTransform.localEulerAngles.x;
            pitch = (camX > 180f) ? camX - 360f : camX;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        yawAngle = transform.eulerAngles.y;

        if (characterRigidbody == null) characterRigidbody = GetComponent<Rigidbody>();

        // Garante menu fechado e tempo rodando no inicio
        if (menuOverlay != null) 
            menuOverlay.SetActive(false);
        
        Time.timeScale = 1f;
    }

    void Update()
    {
        // --- 1. LER ENTRADA DO BOTÃO '+' (PLUS) ANTES DE TUDO ---
        // Precisamos checar isso antes do 'return' de pause, senão não conseguimos fechar o menu.
        if (joycons != null && joycons.Count > camJcIndex && joycons[camJcIndex] != null)
        {
            Joycon jCam = joycons[camJcIndex]; // Geralmente o Joycon Direito tem o botão +
            
            // Botão PLUS para abrir/fechar menu
            if (jCam.GetButtonDown(Joycon.Button.PLUS))
            {
                if (menuOverlay != null)
                {
                    bool isActive = !menuOverlay.activeSelf;
                    menuOverlay.SetActive(isActive);

                    if (isActive)
                    {
                        // === PAUSAR O JOGO ===
                        Time.timeScale = 0f;
                        // Opcional: mostrar cursor se precisar navegar no menu com mouse mesmo usando controle
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        // === DESPAUSAR O JOGO ===
                        Time.timeScale = 1f;
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }

        // --- 2. SE O MENU ESTIVER ABERTO, PARA TUDO ---
        if (menuOverlay != null && menuOverlay.activeSelf)
        {
            currentVelocity = Vector3.zero;
            return; 
        }

        // --- 3. LER STICKS E BOTÕES DE AÇÃO ---
        float moveX = 0f, moveY = 0f;
        float camX = 0f, camY = 0f;

        if (joycons != null)
        {
            // Joycon de Movimento (Esquerdo/Player)
            if (joycons.Count > playerJcIndex && joycons[playerJcIndex] != null)
            {
                Joycon jMove = joycons[playerJcIndex];
                float[] s = jMove.GetStick();
                if (s != null && s.Length >= 2) { moveX = s[0]; moveY = s[1]; }

                // Pulo no Joycon de Movimento (se preferir) OU no de Câmera.
                // Vou manter o pulo no A (DPAD_RIGHT do Joycon Direito) abaixo,
                // mas se quiser pular com setas do Joycon esquerdo, coloque aqui.
            }

            // Joycon de Câmera (Direito)
            if (joycons.Count > camJcIndex && joycons[camJcIndex] != null)
            {
                Joycon jCam = joycons[camJcIndex];
                float[] s = jCam.GetStick();
                if (s != null && s.Length >= 2) { camX = s[0]; camY = s[1]; }

                // --- PULO JOYCON (Botão A = DPAD_RIGHT no Joycon R) ---
                if (characterRigidbody != null)
                {
                    bool isGrounded = false;
                    if (groundCheckTarget != null)
                    {
                        isGrounded = Physics.CheckSphere(groundCheckTarget.position, groundCheckRadius, groundLayer);
                    }

                    if (jCam.GetButtonDown(Joycon.Button.DPAD_RIGHT) && isGrounded)
                    {
                        // Zera velocidade Y para pulo consistente
                        Vector3 vel = characterRigidbody.linearVelocity;
                        characterRigidbody.linearVelocity = new Vector3(vel.x, 0, vel.z);

                        characterRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    }
                }
            }
        }

        // aplicar deadzone
        if (new Vector2(moveX, moveY).magnitude < deadZone) { moveX = moveY = 0f; }
        if (new Vector2(camX, camY).magnitude < deadZone) { camX = camY = 0f; }

        // response curve e sensibilidade
        moveX = ApplyStickResponse(moveX);
        moveY = ApplyStickResponse(moveY);
        camX = ApplyStickResponse(camX);
        camY = ApplyStickResponse(camY);

        moveX *= stickSensitivity;
        moveY *= stickSensitivity;
        camX *= stickSensitivity;
        camY *= stickSensitivity;

        float dt = Time.deltaTime;

        // LOOK via stick (integra delta -> yaw/pitch)
        // Usamos unscaledDeltaTime se quisermos mover câmera no pause, mas aqui o return lá em cima já bloqueia.
        yawAngle = NormalizeAngle(yawAngle + camX * cameraYawSpeed * dt);
        pitch = Mathf.Clamp(pitch - camY * cameraPitchSpeed * dt, minPitch, maxPitch);

        //Movespeed Relacionado a variaveisGlobais
        moveSpeed = variaveisGlobais.velocidade;

        // MOVIMENTO (player-relative)
        Vector3 desiredVelocity = (transform.forward * moveY + transform.right * moveX) * moveSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);

        // aplicar movimento (Rigidbody ou Transform)
        if (characterRigidbody != null)
        {
            Vector3 nextPos = characterRigidbody.position + currentVelocity * dt;
            characterRigidbody.MovePosition(nextPos);
        }
        else
        {
            transform.position += currentVelocity * dt;
        }
    }

    void LateUpdate()
    {
        // aplicar rotação do corpo
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, yawAngle, transform.eulerAngles.z);

        // aplicar pitch na câmera local
        if (cameraTransform != null)
        {
            Vector3 le = cameraTransform.localEulerAngles;
            le.x = pitch;
            cameraTransform.localEulerAngles = le;
        }
    }

    private float ApplyStickResponse(float v)
    {
        float sign = Mathf.Sign(v);
        float mag = Mathf.Abs(Mathf.Clamp(v, -1f, 1f));
        float adjusted = Mathf.Pow(mag, stickResponseExponent);
        return sign * adjusted;
    }

    private float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckTarget.position, groundCheckRadius);
        }
    }
}