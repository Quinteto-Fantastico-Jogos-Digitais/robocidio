using UnityEngine;

/// <summary>
/// PlayerController (WASD + Mouse) - versão que aplica movimento/rotação via Rigidbody em LateUpdate.
/// - Input + smoothing calculados em Update()
/// - MovePosition / MoveRotation chamados em LateUpdate() se houver Rigidbody
/// - Fallback para transform quando não há Rigidbody
/// </summary>
[DisallowMultipleComponent]
public class MainCharacterController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Câmera filha que receberá o pitch (aplique aqui a câmera do jogador).")]
    public Transform cameraTransform;

    [Header("UI Overlay")]
    [Tooltip("Arraste aqui o GameObject (Canvas/Painel) que servirá de menu.")]
    public GameObject menuOverlay; 

    [Header("Movement")]
    public VariavelGlobal variaveisGlobais;
    public float moveSpeed = 10f;
    [Range(0f, 0.5f)] public float moveSmoothTime = 0.08f;

    [Header("Jump")]
    public float jumpForce = 5f;
    [Tooltip("Arraste o objeto vazio 'GroundCheck' que você criou nos pés")]
    public Transform groundCheckTarget;
    public float groundCheckRadius = 0.2f; 
    [Tooltip("Selecione 'Movable' aqui na lista")]
    public LayerMask groundLayer;

    [Header("Mouse Look")]
    public float mouseYawSensitivity = 8.0f;
    public float mousePitchSensitivity = 8.0f;
    public float lookSmoothTime = 0.02f;
    public float minPitch = -45f;
    public float maxPitch = 60f;
    public bool invertY = false;

    [Header("Cursor")]
    [Tooltip("Se true travamos o cursor no Start.")]
    public bool lockCursorOnStart = true;
    [Tooltip("Se true ao clicar com o mouse trancamos o cursor (quando destravado).")]
    public bool lockOnClick = true;

    [Header("Debug / Input")]
    [Tooltip("Magnitude mínima do delta do mouse para considerar 'usando mouse'")]
    public float mouseUseThreshold = 0.001f;

    // internos
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velRef = Vector3.zero;

    private float yawAngle = 0f;    // yaw alvo/suavizado (graus)
    private float pitch = 0f;       // pitch alvo/suavizado (graus)
    private float currentYawVel = 0f;
    private float currentPitchVel = 0f;

    // state
    private float lastMouseMoveTime = -999f;
    public float mouseActiveTimeout = 0.25f;
    public static bool IsUsingMouse { get; private set; } = false;

    // Rigidbody reference (optional)
    Rigidbody rb;

    private bool swordActive = false;

    void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogError("PlayerController: cameraTransform não setado.");
        }

        yawAngle = NormalizeAngle(transform.eulerAngles.y);
        if (cameraTransform != null)
        {
            pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Garante menu fechado e tempo rodando no inicio
        if (menuOverlay != null) 
            menuOverlay.SetActive(false);
        
        Time.timeScale = 1f; // Garante que o jogo não comece pausado

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // --- LÓGICA DO MENU OVERLAY E PAUSE ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuOverlay != null)
            {
                bool isActive = !menuOverlay.activeSelf;
                menuOverlay.SetActive(isActive);

                if (isActive)
                {
                    // === PAUSAR O JOGO ===
                    Time.timeScale = 0f; // Para o tempo
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    // === DESPAUSAR O JOGO ===
                    Time.timeScale = 1f; // Volta o tempo
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // Se o menu estiver aberto, interrompe a leitura de inputs
        if (menuOverlay != null && menuOverlay.activeSelf)
        {
            currentVelocity = Vector3.zero;
            return; 
        }

        // --- Input leitura ---
        // Mouse delta
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        swordActive = Input.GetMouseButton(0) || Input.GetMouseButton(1);

        // --- PULO COM SENSOR NOS PÉS ---
        bool isGrounded = false;
        if (groundCheckTarget != null)
        {
            isGrounded = Physics.CheckSphere(groundCheckTarget.position, groundCheckRadius, groundLayer);
        }

        if (Input.GetKeyDown(KeyCode.Space) && rb != null && isGrounded)
        {
            Vector3 vel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(vel.x, 0, vel.z); 
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // mouse use detection
        if (Mathf.Abs(mx) > mouseUseThreshold || Mathf.Abs(my) > mouseUseThreshold)
        {
            lastMouseMoveTime = Time.time;
            IsUsingMouse = true;
        }
        else
        {
            if (Time.time - lastMouseMoveTime > mouseActiveTimeout)
                IsUsingMouse = false;
        }

        // lock cursor se clicar na tela (somente se menu fechado)
        if (lockOnClick && Cursor.lockState != CursorLockMode.Locked && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- Look calculation ---
        if (!swordActive)
        {
            float targetYaw = yawAngle + mx * mouseYawSensitivity;
            float mouseY = invertY ? -my : my;
            float targetPitch = pitch - mouseY * mousePitchSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Nota: Usamos unscaledDeltaTime para a câmera não travar se o timeScale for mexido em slow motion,
            // mas para pause total (0), ela vai travar mesmo, o que é correto.
            if (lookSmoothTime <= 0f)
            {
                yawAngle = targetYaw;
                pitch = targetPitch;
            }
            else
            {
                yawAngle = Mathf.SmoothDampAngle(yawAngle, targetYaw, ref currentYawVel, lookSmoothTime);
                pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref currentPitchVel, lookSmoothTime);
            }
        }
        

        // --- Movement (WASD) ---
        float hx = Input.GetAxisRaw("Horizontal"); // A/D
        float hy = Input.GetAxisRaw("Vertical");   // W/S

        // movement relative to yawAngle
        Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
        Vector3 forward = yawRot * Vector3.forward;
        Vector3 right = yawRot * Vector3.right;

        moveSpeed = variaveisGlobais.velocidade;
        Vector3 desiredVelocity = (forward * hy + right * hx) * moveSpeed;

        // smooth movement velocity (keeps currentVelocity used to step in LateUpdate)
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);
    }

    void FixedUpdate()
    {
        if (menuOverlay != null && menuOverlay.activeSelf) return;

        Quaternion targetRotation = Quaternion.Euler(0f, yawAngle, 0f);

        //if (rb != null)
        //{
            // MovePosition using currentVelocity * deltaTime (we keep Update computing velocity)
            //Vector3 newPos = rb.position + currentVelocity * Time.deltaTime;
            //rb.MovePosition(newPos);

            // MoveRotation to yaw target (preserve X/Z)
            //rb.MoveRotation(targetRotation);
        //}
        /*else
        {
            // fallback: direct transform changes (as before)
            transform.position += currentVelocity * Time.deltaTime;
            transform.rotation = targetRotation;
        }*/
        // desiredVelocity (calculado no Update) é no espaço do mundo já — se você estiver calculando
        // relativo ao yaw (como no script atual), currentVelocity já está com direção correta.
        // Aplicamos apenas componente XZ ao rb.velocity e preservamos Y (gravidade / pulos).
        
        Vector3 vel = rb.linearVelocity;
        Vector3 desiredVelWorld = currentVelocity; // currentVelocity calculado no Update (unidades/s)
        // preserva Y
        Vector3 newVel = new Vector3(desiredVelWorld.x, vel.y, desiredVelWorld.z);
        rb.linearVelocity = newVel;

        // --- ROTATION (yaw) via MoveRotation para respeitar colisões/physics interpolation ---
        rb.MoveRotation(targetRotation);
        
    }

    // Apply position and rotation in LateUpdate using Rigidbody if present
    void LateUpdate()
    {
        // apply yaw to player body using Rigidbody.MoveRotation if possible
        //Quaternion targetRotation = Quaternion.Euler(0f, yawAngle, 0f);

        /*if (rb != null)
        {
            // MovePosition using currentVelocity * deltaTime (we keep Update computing velocity)
            Vector3 newPos = rb.position + currentVelocity * Time.deltaTime;
            rb.MovePosition(newPos);

            // MoveRotation to yaw target (preserve X/Z)
            rb.MoveRotation(targetRotation);
        }
        else
        {
            // fallback: direct transform changes (as before)
            transform.position += currentVelocity * Time.deltaTime;
            transform.rotation = targetRotation;
        }*/

        // apply pitch to camera child only (preserve camera local X)
        if (cameraTransform != null)
        {
            Vector3 ce = cameraTransform.localEulerAngles;
            ce.x = pitch;
            cameraTransform.localEulerAngles = ce;
        }
    }

    // small utility
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