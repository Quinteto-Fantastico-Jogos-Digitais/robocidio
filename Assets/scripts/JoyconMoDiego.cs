using System.Collections.Generic;
using UnityEngine;
using DynamicMeshCutter;
using System;

/// <summary>
/// JoyconSwordController (versão com CalibrateCenter imediata)
/// </summary>
public class JoyconSwordController : MonoBehaviour
{
    // Variáveis de Hot-Swapping
    private float lastSwitchTime = 0f;
    private const float INPUT_CHECK_WINDOW = 0.5f;

    [Header("Control Scheme")]
    [Tooltip("Define qual esquema de controle está ativo (Joycon ou WASD).")]
    public ControlScheme activeControl = ControlScheme.Joycon;

    [Header("Mouse Input")]
    [Tooltip("Multiplicador da rotação quando usando o mouse para controlar a espada.")]
    public float mouseRotationMultiplier = 5f;

    [Tooltip("Botão do mouse que ativa o controle da espada (1=Direito).")]
    public int mouseLookButton = 1;

    [Header("Joycon")]
    public int jcIndex = 0;               // índice do Joycon (0 = primeiro)
    public bool applyRotation = true;

    [Header("Referências")]
    public Transform calibrationReference; // pointer que segue o corpo (usado para calibrar)
    public Transform playerBody;           // o cilindro/objeto que representa o corpo do jogador
    public PlaneBehaviour cutter;

    [Header("Calibração")]
    public bool useCalibration = true;
    private Quaternion calibOffset = Quaternion.identity; // offset: final = calibOffset * raw

    public float velocidadeParaTrigger = 350f; // deg/s

    // último estado do player para compensar rotação dinâmica
    private Quaternion lastPlayerBodyRotation = Quaternion.identity;
    private bool haveLastPlayerRot = false;

    [Header("Ajustes de rotação")]
    public Vector3 rotationOffsetEuler = Vector3.zero;
    public bool invertRotX = false;
    public bool invertRotY = false;
    public bool invertRotZ = false;

    [Header("Sensibilidade e Dead Zone")]
    [Range(0.1f, 3f)]
    public float rotationSensitivity = 1f;
    [Range(0f, 30f)]
    public float deadZoneDegrees = 1f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float rotationSmoothing = 0.9f; // 0 = sem smoothing (imediato), 1 = muito suave

    // internos
    private List<Joycon> joycons;
    private bool firstFrame = true;
    private Quaternion previousTarget;

    // MOD: novo campo mínimo para o Rigidbody
    Rigidbody swordRb;
    // MOD: armazenar target calculado em Update e aplicado em FixedUpdate
    private Quaternion targetRotation;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();

        calibOffset = Quaternion.identity;
        firstFrame = true;
        previousTarget = transform.rotation;

        swordRb = GetComponent<Rigidbody>();
        Debug.Log(swordRb);

        lastPlayerBodyRotation = playerBody.rotation;
        haveLastPlayerRot = true;
    }

    public void SetControlScheme(ControlScheme newScheme)
    {
        activeControl = newScheme;
        Debug.Log($"[JoyconSwordController] Esquema de controle alterado para: {newScheme}");
    }

    void Update()
    {
        if (joycons == null || joycons.Count <= jcIndex) return;
        Joycon j = joycons[jcIndex];
        if (j == null) return;

        // --- Lógica de Botões para Calibração ---
        if (useCalibration)
        {
            if (j.GetButtonDown(Joycon.Button.SHOULDER_2))
            {
                CalibrateCenter();
            }
            if (j.GetButtonDown(Joycon.Button.SHOULDER_1))
            {
                QuickRecalibrate();
            }
        }
        
        // --- 1. DETECÇÃO E TROCA DE ESQUEMA (HOT-SWAP) ---
        float[] stickArray = j.GetStick();
        Vector2 joyconStick = Vector2.zero;
        if (stickArray != null && stickArray.Length >= 2)
            joyconStick = new Vector2(stickArray[0], stickArray[1]);

        bool joyconButtonInput = 
            j.GetButton(Joycon.Button.DPAD_UP) || 
            j.GetButton(Joycon.Button.SHOULDER_2) || 
            j.GetButton(Joycon.Button.PLUS);
        
        bool joyconInputDetected = joyconStick.magnitude > 0.05f || joyconButtonInput;
        
        bool keyboardMouseInputDetected = 
            Input.GetAxis("Mouse X") != 0 || 
            Input.GetAxis("Mouse Y") != 0 ||
            Input.GetButton("Horizontal") ||
            Input.GetButton("Vertical") ||
            Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            
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

        // --- 2. APLICAÇÃO DA ROTAÇÃO BASEADA NO ESQUEMA ATIVO ---
        
        Quaternion currentTargetRotation;

        if (activeControl == ControlScheme.Joycon)
        {
            // === LÓGICA DO JOYCON: ORIENTAÇÃO ABSOLUTA ===

            if (playerBody != null)
            {
                if (!haveLastPlayerRot)
                {
                    lastPlayerBodyRotation = playerBody.rotation;
                    haveLastPlayerRot = true;
                }
                else
                {
                    if (playerBody.rotation != lastPlayerBodyRotation)
                    {
                        Quaternion delta = playerBody.rotation * Quaternion.Inverse(lastPlayerBodyRotation);
                        calibOffset = delta * calibOffset;
                        lastPlayerBodyRotation = playerBody.rotation;
                    }
                }
            }

            Quaternion rawOrientation = j.GetVector();
            currentTargetRotation = calibOffset * rawOrientation;
        }
        else if (activeControl == ControlScheme.WASD && Input.GetMouseButton(mouseLookButton))
        {
            // === MOUSE BALANÇO ATIVADO PELO BOTÃO DIREITO (DETECÇÃO) ===
            
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float yawDelta = mouseX * mouseRotationMultiplier;
            float pitchDelta = mouseY * mouseRotationMultiplier;

            float absoluteSwing = Mathf.Abs(yawDelta) + Mathf.Abs(pitchDelta);
            
            if (absoluteSwing >= mouseSwingThreshold)
            {
                cutter.Cut();
            }

            // Trava na rotação atual
            currentTargetRotation = swordRb != null ? swordRb.rotation : transform.rotation;
        }
        else // Sem input de rotação
        {
             // Trava na rotação atual
             currentTargetRotation = swordRb != null ? swordRb.rotation : transform.rotation;
        }

        ApplyRotationLogic(currentTargetRotation);
    }

    void FixedUpdate()
    {
        if (!applyRotation) return;

        if (swordRb != null)
        {
            // Garante que a espada siga o Player
            Vector3 targetPosition = transform.parent.position;
            swordRb.MovePosition(targetPosition);

            // Lógica de Rotação
            if (rotationSmoothing <= 0f || firstFrame)
            {
                swordRb.MoveRotation(targetRotation);
            }
            else
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                float t = 1f - Mathf.Pow(1f - rotationSmoothing, dt * 60f);
                Quaternion newRot = Quaternion.Slerp(swordRb.rotation, targetRotation, t);
                swordRb.MoveRotation(newRot);
            }
        }

        if (firstFrame) firstFrame = false;
    }

    private void ApplyRotationLogic(Quaternion baseRotation)
    {
        Quaternion offsetQ = Quaternion.Euler(rotationOffsetEuler);
        Quaternion oriented = baseRotation * offsetQ;

        Quaternion flipQ = Quaternion.Euler(invertRotX ? 180f : 0f, invertRotY ? 180f : 0f, invertRotZ ? 180f : 0f);
        oriented = oriented * flipQ;

        Vector3 e = oriented.eulerAngles;
        e.x = (e.x > 180f) ? e.x - 360f : e.x;
        e.y = (e.y > 180f) ? e.y - 360f : e.y;
        e.z = (e.z > 180f) ? e.z - 360f : e.z;

        e *= rotationSensitivity;
        e.x = Mathf.Clamp(e.x, -180f, 180f);
        e.y = Mathf.Clamp(e.y, -180f, 180f);
        e.z = Mathf.Clamp(e.z, -180f, 180f);

        Quaternion candidateTarget = Quaternion.Euler(e);

        // Normalize (Correção do erro de unit length)
        candidateTarget = Quaternion.Normalize(candidateTarget);

        // dead zone: mantém previous se mudança pequena
        Quaternion target;
        if (!firstFrame)
        {
            float angleDiff = Quaternion.Angle(previousTarget, candidateTarget);
            if (angleDiff < deadZoneDegrees)
                target = previousTarget;
            else
                target = candidateTarget;
        }
        else
        {
            target = candidateTarget;
        }

        previousTarget = target;
        targetRotation = target;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (joycons == null || joycons.Count <= jcIndex) return;

        Joycon joy = joycons[jcIndex];

        Debug.Log("Colidi com: " + other.gameObject.name);

        joy.SetRumble(80, 160, 0.6f, 100);
        cutter.Cut();
        
    }

    public void CalibrateCenter()
    {
        if (calibrationReference == null)
        {
            Debug.LogWarning("[JoyconSwordController] CalibrateCenter: calibrationReference is null.");
            return;
        }

        Quaternion raw = joycons != null && joycons.Count > jcIndex && joycons[jcIndex] != null
            ? joycons[jcIndex].GetVector()
            : Quaternion.identity;

        calibOffset = calibrationReference.rotation * Quaternion.Inverse(raw);
        transform.rotation = calibrationReference.rotation;

        previousTarget = transform.rotation;
        firstFrame = false;

        if (playerBody != null)
        {
            lastPlayerBodyRotation = playerBody.rotation;
            haveLastPlayerRot = true;
        }

        Debug.Log($"[JoyconSwordController] CalibrateCenter executed. jcIndex={jcIndex}");
    }

    public void QuickRecalibrate()
    {
        Quaternion raw = joycons != null && joycons.Count > jcIndex && joycons[jcIndex] != null
            ? joycons[jcIndex].GetVector()
            : Quaternion.identity;

        calibOffset = transform.rotation * Quaternion.Inverse(raw);

        if (playerBody != null)
        {
            lastPlayerBodyRotation = playerBody.rotation;
            haveLastPlayerRot = true;
        }

        Debug.Log($"[JoyconSwordController] QuickRecalibrate executed. jcIndex={jcIndex}");
    }
}