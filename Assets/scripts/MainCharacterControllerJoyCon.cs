using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JoyconPlayerController_JoyconOnly
/// - Somente entradas via Joycon sticks (GetStick()).
/// - Sem mouse, sem keyboard.
/// - Mantém variáveis de tuning similares ao seu script anterior.
/// - Usa Rigidbody.MovePosition se existir, caso contrário altera transform.position.
/// </summary>
[DisallowMultipleComponent]
public class MainCharacterControllerJoyCon : MonoBehaviour
{
    [Header("Joycon indices")]
    public int playerJcIndex = 0;   // stick de movimento
    public int camJcIndex = 1;      // stick de olhar (yaw/pitch)

    [Header("References")]
    public Transform cameraTransform; // atribua a câmera filha (ou deixe vazio para usar Camera.main)
    public Rigidbody characterRigidbody; // opcional: se presente, MovePosition será usado

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float moveSmoothTime = 0.08f;

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
    }

    void Update()
    {
        // ler sticks somente via Joycon
        float moveX = 0f, moveY = 0f;
        float camX = 0f, camY = 0f;

        if (joycons != null)
        {
            if (joycons.Count > playerJcIndex && joycons[playerJcIndex] != null)
            {
                float[] s = joycons[playerJcIndex].GetStick();
                if (s != null && s.Length >= 2) { moveX = s[0]; moveY = s[1]; }
            }

            if (joycons.Count > camJcIndex && joycons[camJcIndex] != null)
            {
                float[] s = joycons[camJcIndex].GetStick();
                if (s != null && s.Length >= 2) { camX = s[0]; camY = s[1]; }
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
        yawAngle = NormalizeAngle(yawAngle + camX * cameraYawSpeed * dt);
        pitch = Mathf.Clamp(pitch - camY * cameraPitchSpeed * dt, minPitch, maxPitch);

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
}
