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
    public Transform cameraTransform; // child camera; fallback Camera.main

    [Header("Movement")]
    public float moveSpeed = 4f;            // unidades/segundo
    [Range(0f, 0.5f)] public float moveSmoothTime = 0.08f;

    [Header("Mouse Look")]
    [Tooltip("Multiplicador horizontal do mouse (graus por unidade)")]
    public float mouseYawSensitivity = 8.0f;
    [Tooltip("Multiplicador vertical do mouse (graus por unidade)")]
    public float mousePitchSensitivity = 8.0f;
    [Tooltip("Tempo de suavização para o look (menor = mais responsivo).")]
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
                Debug.LogError("PlayerController: cameraTransform não setado e Camera.main não encontrada.");
        }

        // inicializa yaw/pitch com os valores atuais do transform
        yawAngle = NormalizeAngle(transform.eulerAngles.y);
        if (cameraTransform != null)
        {
            pitch = NormalizeAngle(cameraTransform.localEulerAngles.x);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // cursor
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // cache rigidbody se existir
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // para este estilo de controle, kinematic ou non-kinematic ambos funcionam com MovePosition,
            // mas recomendamos isKinematic=true se não usar física real de colisão/resposta de força.
            // Não forçamos nada aqui — deixamos você controlar no inspector.
        }
    }

    void Update()
    {
        // --- Input leitura ---
        // Mouse delta
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        swordActive = Input.GetMouseButton(0) || Input.GetMouseButton(1);

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

        // lock/unlock cursor controls
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (lockOnClick && Cursor.lockState != CursorLockMode.Locked && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- Look calculation (not applied yet) ---
        if (!swordActive)
        {
            float targetYaw = yawAngle + mx * mouseYawSensitivity;
            float mouseY = invertY ? -my : my;
            float targetPitch = pitch - mouseY * mousePitchSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

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

        Vector3 desiredVelocity = (forward * hy + right * hx) * moveSpeed;

        // smooth movement velocity (keeps currentVelocity used to step in LateUpdate)
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velRef, moveSmoothTime);
    }

    void FixedUpdate()
    {
        // apply yaw to player body using Rigidbody.MoveRotation if possible
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
}
