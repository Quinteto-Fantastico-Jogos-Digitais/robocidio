using System.Collections.Generic;
using UnityEngine;
using DynamicMeshCutter;
using System;

/// <summary>
/// JoyconSwordController (versão com CalibrateCenter imediata + suporte a mouse para index 0/1)
/// - Right mouse -> controla sword index 0
/// - Left mouse  -> controla sword index 1
/// - Primeiro clique (por botão) força CalibrateCenter() para mover a espada pra frente
/// - Enquanto o mouse estiver ativo suprimimos leituras de orientação do Joycon (GetVector)
/// </summary>
public class JoyconSwordController1 : MonoBehaviour
{
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

    // parâmetros mouse -> joy simulation (mínimos, editáveis)
    [Header("Mouse -> Sword")]
    [Tooltip("Angular speed (deg/s) applied to simulated joy quaternion from mouse deltas.")]
    public float mouseToJoyAngularSpeed = 180f;
    [Tooltip("Quanto tempo (s) consideramos que o mouse está ativo após último movimento.")]
    public float mouseActiveTimeout = 0.25f;

    // internos
    private List<Joycon> joycons;
    private bool firstFrame = true;
    private Quaternion previousTarget;

    // MOD: novo campo mínimo para o Rigidbody
    Rigidbody swordRb;
    // MOD: armazenar target calculado em Update e aplicado em FixedUpdate
    private Quaternion targetRotation;

    // mouse/flag state
    private float lastMouseMoveTime = -10f;
    public static bool IsMouseCapturingSword = false; // PUBLIC estático para que PlayerMovement possa checar
    public static int MouseCapturingIndex = -1; // -1 = nenhum / ambas = -1 (ou se só uma, 0/1)
    private Quaternion simulatedJoyQuat = Quaternion.identity; // quando em modo mouse controlamos essa quat

    // primeiro clique por espada já calibrado?
    private bool forcedCalibratedOnce = false;

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();
        calibOffset = Quaternion.identity;
        firstFrame = true;
        previousTarget = transform.rotation;

        // MOD
        swordRb = GetComponent<Rigidbody>();
        Debug.Log(swordRb);

        lastPlayerBodyRotation = playerBody != null ? playerBody.rotation : Quaternion.identity;
        haveLastPlayerRot = (playerBody != null);

        // inicializa simulatedJoyQuat a partir do joycon (se existir) ou do transform atual
        Joycon j0 = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;
        if (j0 != null)
            simulatedJoyQuat = j0.GetVector();
        else
            simulatedJoyQuat = transform.rotation;
    }

    void Update()
    {
        // leitura de mouse (não é invasiva aqui)
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");
        bool mouseMoved = Mathf.Abs(mouseX) > 0f || Mathf.Abs(mouseY) > 0f;

        if (mouseMoved)
            lastMouseMoveTime = Time.time;

        bool rightMouseHeld = Input.GetMouseButton(1); // botão direito
        bool leftMouseHeld = Input.GetMouseButton(0);  // botão esquerdo

        // se houve o clique inicial (first down) para esse botão/index, forçamos calibração
        if (Input.GetMouseButtonDown(1) && jcIndex == 0 && !forcedCalibratedOnce)
        {
            // calibrar a espada direita na primeira vez que clicar com o direito
            CalibrateCenter();
            forcedCalibratedOnce = true;
        }
        if (Input.GetMouseButtonDown(0) && jcIndex == 1 && !forcedCalibratedOnce)
        {
            // calibrar a espada esquerda na primeira vez que clicar com o esquerdo
            CalibrateCenter();
            forcedCalibratedOnce = true;
        }

        // ---------- leitura segura do Joycon (sem sair precoce) ----------
        Joycon j = (joycons != null && joycons.Count > jcIndex) ? joycons[jcIndex] : null;

        // leitura de botões para calibração: mantemos (botões provêm do joycon; se j==null nada acontece)
        if (useCalibration && j != null)
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

        // atualiza calibOffset se o player body rotacionou (compensação dinâmica)
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

        // ---------- DECISÃO: usamos JOYCON OU SIMULATION (mouse) para ORIENTAÇÃO ----------
        bool mouseActive = (Time.time - lastMouseMoveTime) <= mouseActiveTimeout;

        // Determina se este script (esta instância) deve capturar o mouse:
        bool capturingThisScript = (rightMouseHeld && jcIndex == 0) || (leftMouseHeld && jcIndex == 1);

        // Atualiza flags estáticas (global)
        bool anyCapture = rightMouseHeld || leftMouseHeld;
        IsMouseCapturingSword = anyCapture;

        // define MouseCapturingIndex: se apenas um botão pressionado, seta 0 ou 1; se ambos ou nenhum, -1
        if (rightMouseHeld && !leftMouseHeld) MouseCapturingIndex = 0;
        else if (leftMouseHeld && !rightMouseHeld) MouseCapturingIndex = 1;
        else MouseCapturingIndex = -1;

        Quaternion rawOrientation = Quaternion.identity;

        if (capturingThisScript)
        {
            // botão associado a esta instância está PRESO => usamos delta do mouse para controlar a espada
            float dt = Mathf.Max(0.0001f, Time.deltaTime);

            // mapear: mouse X -> yaw, mouse Y -> pitch (inverte pitch para movimento natural)
            float yawDeg = mouseX * mouseToJoyAngularSpeed * dt;
            float pitchDeg = -mouseY * mouseToJoyAngularSpeed * dt;

            Quaternion delta = Quaternion.Euler(pitchDeg, yawDeg, 0f);
            simulatedJoyQuat = delta * simulatedJoyQuat;

            rawOrientation = simulatedJoyQuat;
        }
        else if (mouseActive)
        {
            // mouse moveu recentemente, mas esta instância NÃO está capturando (ou está apenas movendo sem botão):
            // suprimimos chamadas a GetVector() para evitar leituras do Joycon enquanto o mouse está ativo.
            rawOrientation = simulatedJoyQuat; // mantemos último estado simulado/real
        }
        else
        {
            // mouse inativo: usamos o joycon real (se existir).
            if (j != null)
            {
                rawOrientation = j.GetVector();
                // sincroniza a simulação com o estado real para que a transição mouse<->joy não salte
                simulatedJoyQuat = rawOrientation;
            }
            else
            {
                // nenhum joycon: mantém a simulatedJoyQuat (padrão)
                rawOrientation = simulatedJoyQuat;
            }
        }

        // ---------- aplica calibração e offsets (igual ao seu fluxo original) ----------
        Quaternion oriented = calibOffset * rawOrientation;

        Quaternion offsetQ = Quaternion.Euler(rotationOffsetEuler);
        oriented = oriented * offsetQ;

        Quaternion flipQ = Quaternion.Euler(invertRotX ? 180f : 0f, invertRotY ? 180f : 0f, invertRotZ ? 180f : 0f);
        oriented = oriented * flipQ;

        // Convertemos para Euler apenas para aplicar sensibilidade e clamp simples
        Vector3 e = oriented.eulerAngles;
        e.x = (e.x > 180f) ? e.x - 360f : e.x;
        e.y = (e.y > 180f) ? e.y - 360f : e.y;
        e.z = (e.z > 180f) ? e.z - 360f : e.z;

        // aplica sensibilidade (escala angular)
        /*e *= rotationSensitivity;
        e.x = Mathf.Clamp(e.x, -180f, 180f);
        e.y = Mathf.Clamp(e.y, -180f, 180f);
        e.z = Mathf.Clamp(e.z, -180f, 180f);*/
        e *= rotationSensitivity;
        e.x = Mathf.Clamp(e.x, -180f, 180f);
        e.y = Mathf.Clamp(e.y, -180f, 180f);
        e.z = Mathf.Clamp(e.z, -180f, 180f);

        // LIMITES ESPECÍFICOS (mínima alteração)
        // X entre -50 e 50, Y entre -100 e 100, Z sem limite adicional
        e.x = Mathf.Clamp(e.x, -50f, 50f);
        e.y = Mathf.Clamp(e.y, -100f, 100f);

        // candidate target
        //Quaternion candidateTarget = Quaternion.Euler(e);
                // dead zone: mantém previous se mudança pequena
        /*Quaternion target;
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
        targetRotation = target;*/
        // // ---------- cria candidateTarget de forma "continua" evitando saltos por wrap de Euler ----------
        // já temos 'e' com os clamps aplicados (incluindo seus limites X/Y/Z)

        // cria o candidate base
        Quaternion baseCandidate = Quaternion.Euler(e);

        // procura a melhor variante adicionando/subtraindo 360 em cada eixo para minimizar a diferença angular
        Quaternion bestCandidate = baseCandidate;
        float bestAngle = Quaternion.Angle(previousTarget, baseCandidate);

        // testar variações -360, 0, +360 em cada eixo (27 combinações)
        float[] shifts = new float[] { -360f, 0f, 360f };
        for (int ix = 0; ix < 3; ix++)
        {
            for (int iy = 0; iy < 3; iy++)
            {
                for (int iz = 0; iz < 3; iz++)
                {
                    Vector3 ev = new Vector3(e.x + shifts[ix], e.y + shifts[iy], e.z + shifts[iz]);
                    Quaternion q = Quaternion.Euler(ev);
                    float ang = Quaternion.Angle(previousTarget, q);
                    if (ang < bestAngle)
                    {
                        bestAngle = ang;
                        bestCandidate = q;
                    }
                }
            }
        }

        // agora bestCandidate é o candidato "mais próximo" do previousTarget
        Quaternion candidateTarget = bestCandidate;

        // dead zone: mantém previous se mudança pequena (usa o mesmo critério seu)
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

        // fazer ela seguir o pai (como você já tinha)
        if (transform.parent != null)
            transform.position = transform.parent.position;
        else
            transform.position = transform.position; // sem alteração

        // NOTA: NÃO setamos firstFrame=false aqui — fazemos na FixedUpdate
    }

    void FixedUpdate()
    {
        // MOD: aplicamos a rotação aqui via Rigidbody (se existir), senão caímos de volta para transform.rotation
        if (!applyRotation) return;

        if (swordRb != null)
        {
            if (rotationSmoothing <= 0f || firstFrame)
            {
                swordRb.MoveRotation(targetRotation);
            }
            else
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                float t = 1f - Mathf.Pow(1f - rotationSmoothing, dt * 60f); // mesma fórmula, com fixedDeltaTime
                Quaternion newRot = Quaternion.Slerp(swordRb.rotation, targetRotation, t);
                swordRb.MoveRotation(newRot);
            }
        }
        else
        {
            // fallback sem rigidbody
            if (rotationSmoothing <= 0f || firstFrame)
                transform.rotation = targetRotation;
            else
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                float t = 1f - Mathf.Pow(1f - rotationSmoothing, dt * 60f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }
        }

        // MOD: após a primeira aplicação em FixedUpdate, marcamos que não é mais o primeiro frame
        if (firstFrame) firstFrame = false;
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

        // raw atual do Joy-Con (proteção)
        Quaternion raw = joycons != null && joycons.Count > jcIndex && joycons[jcIndex] != null
            ? joycons[jcIndex].GetVector()
            : Quaternion.identity;

        // definimos calibOffset tal que: calibOffset * raw = calibrationReference.rotation
        calibOffset = calibrationReference.rotation * Quaternion.Inverse(raw);

        // **APLICAÇÃO IMEDIATA**: faz a espada apontar para a calibrationReference agora
        transform.rotation = calibrationReference.rotation;

        // atualiza estados internos para que smoothing / deadzone não "ignore" esta mudança
        previousTarget = transform.rotation;
        firstFrame = false;

        // atualiza lastPlayerBodyRotation para evitar salto imediato na compensação dinâmica
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
