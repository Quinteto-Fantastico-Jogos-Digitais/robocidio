using System.Collections.Generic;
using UnityEngine;
using DynamicMeshCutter;

/// <summary>
/// JoyconWeaponController_JoyconOnly
/// - Somente Joycon.GetVector() e botões Joycon para captura/ações.
/// - Não usa mouse/keyboard nem define flags que parem a câmera.
/// - Mantém funções de calibração e rumble via Joycon API.
/// </summary>
[DisallowMultipleComponent]
public class WeaponControllerJoyCon : MonoBehaviour
{
    [Header("Joycon")]
    public int jcIndex = 0;
    public bool applyRotation = true;

    [Header("References")]
    public Transform handTransform;   // posição onde arma segue
    public Transform handlePivot;     // pivot que gira (mesh deve estar sob ele)
    public Camera came;
    public Transform playerBody;      // usado para construir target global
    public PlaneBehaviour cutter;

    [Header("Rotation / tuning")]
    public float rotationSensitivity = 1f;
    [Range(0f, 0.99f)]
    public float rotationSmoothing = 0.9f;
    public Vector3 rotationOffsetEuler = Vector3.zero;
    public bool invertRotX = false;
    public bool invertRotY = false;
    public bool invertRotZ = false;

    [Header("Local limits")]
    public float limitPitchMin = -50f;
    public float limitPitchMax = 50f;
    public float limitYawMin = -100f;
    public float limitYawMax = 100f;

    [Header("Safety")]
    public float maxLocalDegPerSecond = 720f;
    public float deadZoneDegrees = 1f;

    // internals
    private List<Joycon> joycons;
    private Rigidbody rb;
    private Quaternion calibOffset = Quaternion.identity;
    private Quaternion previousTarget = Quaternion.identity;
    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion lastJoyQuat = Quaternion.identity;
    private bool firstFrame = true;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();
        rb = GetComponent<Rigidbody>();

        // init lastJoyQuat from actual Joycon if present, else from current transform
        Joycon j = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;
        if (j != null) lastJoyQuat = j.GetVector();
        else lastJoyQuat = (handlePivot != null) ? handlePivot.rotation : transform.rotation;

        previousTarget = (handlePivot != null) ? handlePivot.rotation : transform.rotation;
    }

    void Update()
    {
        Joycon j = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;

        if (j.GetButtonDown(Joycon.Button.SHOULDER_2))
        {

            /*Quaternion raw = j.GetVector();
            Transform cam = pointer.transform;
            Quaternion desired = cam.rotation;

            calibOffset = desired * Quaternion.Inverse(raw);
            Debug.Log(desired);*/

            Quaternion raw = j.GetVector();
            Transform cam = came.transform;
            Quaternion desired = Quaternion.LookRotation(cam.forward, cam.up); // orientação limpa da câmera
            calibOffset = desired * Quaternion.Inverse(raw);
            Debug.Log("[Calib] desired (world cam): " + desired.eulerAngles);

        }

        // Prefer Joycon data only. If não houver Joycon, mantém última quat conhecida (no últimoJoyQuat).
        Quaternion rawOrientation = (j != null) ? j.GetVector() : lastJoyQuat;
        lastJoyQuat = rawOrientation;

        // aplicar calibração/offsets
        Quaternion oriented = calibOffset * rawOrientation;
        oriented = oriented * Quaternion.Euler(rotationOffsetEuler);

        // aplicar inverts via multiplicação por 180 em eixos (se necessário)
        Quaternion flipQ = Quaternion.Euler(invertRotX ? 180f : 0f, invertRotY ? 180f : 0f, invertRotZ ? 180f : 0f);
        oriented = oriented * flipQ;

        // converter para euler e ajustar limites locais
        Vector3 e = oriented.eulerAngles;
        e.x = (e.x > 180f) ? e.x - 360f : e.x;
        e.y = (e.y > 180f) ? e.y - 360f : e.y;
        e.z = (e.z > 180f) ? e.z - 360f : e.z;

        // segurança: evita valores estranhos por excesso de sensibilidade
        //e *= rotationSensitivity;
        //e.x = Mathf.Clamp(e.x, limitPitchMin, limitPitchMax);
        //e.y = Mathf.Clamp(e.y, limitYawMin, limitYawMax);
        e *= rotationSensitivity;
        e.x = Mathf.Clamp(e.x, -180f, 180f);
        e.y = Mathf.Clamp(e.y, -180f, 180f);
        e.z = Mathf.Clamp(e.z, -180f, 180f);
        
        Quaternion candidate = Quaternion.Euler(e);

        // choose candidate closest to previous target to avoid jumps (wrap handling)
        Quaternion best = candidate;
        /*float bestAngle = Quaternion.Angle(previousTarget, candidate);
        float[] shifts = new float[] { -360f, 0f, 360f };
        for (int ix = 0; ix < 3; ix++)
            for (int iy = 0; iy < 3; iy++)
                for (int iz = 0; iz < 3; iz++)
                {
                    Vector3 ev = new Vector3(e.x + shifts[ix], e.y + shifts[iy], e.z + shifts[iz]);
                    Quaternion q = Quaternion.Euler(ev);
                    float ang = Quaternion.Angle(previousTarget, q);
                    if (ang < bestAngle) { bestAngle = ang; best = q; }
                }*/

        Quaternion targetLocal = best;

        // dead zone
        if (!firstFrame)
        {
            float angleDiff = Quaternion.Angle(previousTarget, targetLocal);
            if (angleDiff < deadZoneDegrees) targetLocal = previousTarget;
        }

        // limit angular speed
        if (!firstFrame && maxLocalDegPerSecond > 0f)
        {
            float angleNow = Quaternion.Angle(previousTarget, targetLocal);
            float maxDelta = maxLocalDegPerSecond * Time.deltaTime;
            if (angleNow > maxDelta && angleNow > 0.0001f)
            {
                float frac = maxDelta / angleNow;
                targetLocal = Quaternion.Slerp(previousTarget, targetLocal, frac);
            }
        }

        previousTarget = targetLocal;

        // target no mundo (opcionalmente relativo ao playerBody)
        if (playerBody != null) targetRotation = playerBody.rotation * targetLocal;
        else targetRotation = targetLocal;

        firstFrame = false;

        // seguir a posição da mão (apenas posição)
        if (handTransform != null)
            transform.position = handTransform.position;

    }

    void FixedUpdate()
    {
        // seguir a posição da mão (apenas posição)
        /*if (handTransform != null)
            transform.position = handTransform.position;*/

        if (!applyRotation) return;

        if (rotationSmoothing <= 0f)
            rb.MoveRotation(targetRotation);
        else
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            float t = 1f - Mathf.Pow(1f - rotationSmoothing, dt * 60f);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRotation, t);
            rb.MoveRotation(newRot);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!this.enabled) return;

        // rumble de aviso (se Joycon presente)
        Joycon j = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;
        if (j != null)
        {
            j.SetRumble(80, 160, 0.6f, 100);
            cutter.Cut();

            //Se for inimigo chama a função de morrer
            if (other.gameObject.GetComponentInParent<EnemyAI>() != null)
            {
                //UnityEngine.Debug.Log("matou o veio");
                other.gameObject.GetComponentInParent<EnemyAI>().die();
            }

        }
    }
}
