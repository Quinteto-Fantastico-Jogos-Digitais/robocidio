using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class SwingController : MonoBehaviour
{
    [Header("Joycon")]
    public int jcIndex = 0;
    private List<Joycon> joycons;
    private Joycon jc;

    [Header("Offsets")]
    public Vector3 positionOffsetLocal = Vector3.zero;
    public Vector3 eulerRotationOffsetLocal = Vector3.zero;

    [Header("Smoothing (kinematic)")]
    [Tooltip("Base smoothing speed (higher = more rigid, less lag).")]
    public float rotationSmoothBase = 18f;
    [Tooltip("Smoothing speed used quando movimento rápido (maior => segue mais imediatamente).")]
    public float rotationSmoothFast = 60f;
    [Tooltip("Gyro magnitude que considera 'movimento rápido' (unidade do plugin, tipicamente deg/s).")]
    public float gyroFastThreshold = 250f;
    [Tooltip("Position smoothing speed (for local offset follow).")]
    public float positionSmooth = 12f;
    public float rotationDeadAngle = 0.6f;
    public float positionDeadDistance = 0.005f;

    [Header("Gyro-based prediction")]
    [Tooltip("Multiplicador para prever a orientação com base no gyro; valores pequenos (0.01-0.05) reduzem sensação de lag.")]
    [Range(0f, 0.25f)]
    public float gyroPredictionFactor = 0.03f;

    [Header("Calibration / Auto recenter")]
    public bool useCalibration = true;
    public bool enableAutoRecenter = true;
    public KeyCode calibrationKey = KeyCode.C;
    public float gyroIdleThreshold = 35f;
    public float stickIdleThreshold = 0.12f;
    public float idleTimeToRecenter = 1.2f;
    private float idleTimer = 0f;
    private Quaternion calibOffset = Quaternion.identity;

    [Header("Physics (optional)")]
    [Tooltip("Se true, tentaremos usar torque para aproximar a espada (útil apenas se quiser física).")]
    public bool usePhysicsJoint = false;
    [Tooltip("Rigidbody da espada (necessário se usePhysicsJoint = true).")]
    public Rigidbody swordRigidbody;
    [Tooltip("Força de torque aplicada para alinhar a espada (fixar em valores baixos e ajustar).")]
    public float jointTorque = 25f;
    [Tooltip("Damping do torque (reduz oscillação).")]
    public float jointDamping = 5f;

    [Header("Debug")]
    public bool debugLog = false;

    // internals
    private Transform parentT;
    private Quaternion lastSmoothedRotation;
    private Vector3 lastSmoothedPosition;

    // internos (adicionar)
    private bool hasSwingOverride = false;
    private Quaternion swingOverrideRot = Quaternion.identity;
    private float swingOverrideStrength = 0f;

    /// <summary>Chamado por SwingContinuation para forçar a continuação do golpe.</summary>
    public void ApplySwingOverride(Quaternion overrideRotation, float strength)
    {
        hasSwingOverride = true;
        swingOverrideRot = overrideRotation;
        swingOverrideStrength = Mathf.Clamp01(strength);
    }

    /// <summary>Limpa o override do swing.</summary>
    public void ClearSwingOverride()
    {
        hasSwingOverride = false;
        swingOverrideStrength = 0f;
    }

    void Start()
    {
        parentT = transform.parent != null ? transform.parent : transform;
        lastSmoothedRotation = transform.rotation;
        lastSmoothedPosition = transform.position;

        // inicializa offsets com transform atual (bom para não precisar calibrar posição)
        positionOffsetLocal = transform.localPosition;
        eulerRotationOffsetLocal = transform.localRotation.eulerAngles;

        if (jcIndex >= 0 && JoyconManager.Instance != null)
        {
            joycons = JoyconManager.Instance.j;
            if (joycons != null && joycons.Count > jcIndex) jc = joycons[jcIndex];
        }

        calibOffset = Quaternion.identity;
    }

    void LateUpdate()
    {
        // atualiza jc se desconectado
        if (jc == null && jcIndex >= 0 && JoyconManager.Instance != null)
        {
            joycons = JoyconManager.Instance.j;
            if (joycons != null && joycons.Count > jcIndex) jc = joycons[jcIndex];
        }

        // manual calibration
        if (useCalibration)
        {
            bool manualPressed = (jc != null && jc.GetButtonDown(Joycon.Button.SHOULDER_2)) || Input.GetKeyDown(calibrationKey);
            if (manualPressed)
            {
                Quaternion raw = jc != null ? jc.GetVector() : Quaternion.identity;
                calibOffset = Quaternion.Inverse(raw);
                if (debugLog) Debug.Log("[SmoothSwordController] Manual calib.");
            }
        }

        // compute raw orientation and apply calibration
        Quaternion rawOrientation = (jc != null) ? jc.GetVector() : transform.parent != null ? transform.parent.rotation * Quaternion.Euler(eulerRotationOffsetLocal) : transform.rotation;
        Quaternion oriented = calibOffset * rawOrientation;
        Quaternion offsetRot = Quaternion.Euler(eulerRotationOffsetLocal);
        Quaternion desiredWorldRot = (transform.parent != null) ? transform.parent.rotation * (oriented * offsetRot) : (oriented * offsetRot);

        // gyro magnitude for adaptive smoothing / prediction
        Vector3 gyro = jc != null ? jc.GetGyro() : Vector3.zero;
        float gyroMag = gyro.magnitude;

        // PREDICTION: rotate a bit in direction of gyro to reduce visual lag
        // NOTE: assume gyro units are degrees/second; if plugin uses rad/s, reduce factor accordingly
        Quaternion gyroPrediction = Quaternion.identity;
        if (gyroPredictionFactor > 0f && jc != null)
        {
            // small-angle approximation: build small euler from gyro * factor (dt already not included; we want short-time prediction)
            // use world-space axis from device gyro — it's an approximation but works well visually.
            Vector3 predictedEuler = gyro * gyroPredictionFactor; // degrees
            gyroPrediction = Quaternion.Euler(predictedEuler);
            desiredWorldRot = desiredWorldRot * gyroPrediction;
        }

        // ADAPTIVE SMOOTH: increase responsiveness when gyro is large
        float smoothSpeed = Mathf.Lerp(rotationSmoothBase, rotationSmoothFast, Mathf.Clamp01(gyroMag / gyroFastThreshold));
        float tRot = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        // EARLY EXIT rotation if almost equal
        float angleDiff = Quaternion.Angle(lastSmoothedRotation, desiredWorldRot);
        if (angleDiff > rotationDeadAngle)
        {
            // slerp from lastSmoothedRotation to desiredWorldRot, store as lastSmoothedRotation
            lastSmoothedRotation = Quaternion.Slerp(lastSmoothedRotation, desiredWorldRot, tRot);
        }
        // else keep lastSmoothedRotation

        // POSITION: keep local offset relative to parent
        Vector3 desiredLocalPos = positionOffsetLocal;
        Vector3 desiredWorldPos = transform.parent != null ? transform.parent.TransformPoint(desiredLocalPos) : desiredLocalPos;
        float tPos = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        float dist = Vector3.Distance(lastSmoothedPosition, desiredWorldPos);
        if (dist > positionDeadDistance)
            lastSmoothedPosition = Vector3.Lerp(lastSmoothedPosition, desiredWorldPos, tPos);

        // se houver override do swing, mescle
        if (hasSwingOverride)
        {
            Debug.Log("fez o swing");
            // blend entre smoothed e override (overrideStrength indica quanto domina)
            Quaternion finalRot = Quaternion.Slerp(lastSmoothedRotation, swingOverrideRot, swingOverrideStrength);
            // kinematic: set transform using smoothed values (fast, predictable)
            transform.position = lastSmoothedPosition;
            transform.rotation = finalRot;
        }
        else
        {
            // kinematic: set transform using smoothed values (fast, predictable)
            transform.position = lastSmoothedPosition;
            transform.rotation = lastSmoothedRotation;
        }
    }

}

