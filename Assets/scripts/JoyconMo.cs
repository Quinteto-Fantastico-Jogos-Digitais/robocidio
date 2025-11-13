using System.Collections.Generic;
using UnityEngine;
using DynamicMeshCutter;

/// <summary>
/// JoyconSwordController (versão com CalibrateCenter imediata)
/// </summary>
public class JoyconSwordController : MonoBehaviour
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

    // internos
    private List<Joycon> joycons;
    private bool firstFrame = true;
    private Quaternion previousTarget;

    // MOD: novo campo mínimo para o Rigidbody
    Rigidbody swordRb;
    // MOD: armazenar target calculado em Update e aplicado em FixedUpdate
    private Quaternion targetRotation;

    public float pushStrength = 2f;        // ajuste fino
    public float pushUpFactor = 0.1f;      // empurra um pouco pra cima
    public bool useAddForceAtPoint = true;

    //Lista de dados que verifica hit
    private List<int> sides = new List<int> { 1, 2, 3, 4, 5, 6 };

    void Start()
    {
        joycons = (JoyconManager.Instance != null) ? JoyconManager.Instance.j : new List<Joycon>();
        calibOffset = Quaternion.identity;
        firstFrame = true;
        previousTarget = transform.rotation;

        // MOD
        swordRb = GetComponent<Rigidbody>();
        Debug.Log(swordRb);

        lastPlayerBodyRotation = playerBody.rotation;
        haveLastPlayerRot = true;
        
    }

    void Update()
    {
        // segurança: precisa existir Joycon no index
        if (joycons == null || joycons.Count <= jcIndex) return;
        Joycon j = joycons[jcIndex];
        if (j == null) return;

        // leitura de botões para calibração
        if (useCalibration)
        {
            // Calibração "centralizante" (aponta a espada para a referência atual)
            if (j.GetButtonDown(Joycon.Button.SHOULDER_2))
            {
                CalibrateCenter();
            }

            // Recalibração rápida (ajusta offset sem alterar visualmente a espada)
            if (j.GetButtonDown(Joycon.Button.SHOULDER_1))
            {
                QuickRecalibrate();
            }
        }

        // Debugg
        /*if (j.GetButtonDown(Joycon.Button.DPAD_UP))
        {
            Debug.Log(j.GetGyro().magnitude);
            cutter.Cut();
        }
        //MOVI PARA ONCOLISSION DETECTION
        if (j.GetGyro().magnitude >= velocidadeParaTrigger)
        {
            Debug.Log("Trigou");
            j.SetRumble(80, 160, 0.6f, 50);
            cutter.Cut();
        }*/

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

        // pega quaternion bruto do Joycon
        Quaternion rawOrientation = j.GetVector();

        // aplica calibração (offset)
        Quaternion oriented = calibOffset * rawOrientation;

        // aplica offset do modelo (ajuste em Euler)
        Quaternion offsetQ = Quaternion.Euler(rotationOffsetEuler);
        oriented = oriented * offsetQ;

        // aplica inversões por eixo (usando um flip quaternion simples)
        Quaternion flipQ = Quaternion.Euler(invertRotX ? 180f : 0f, invertRotY ? 180f : 0f, invertRotZ ? 180f : 0f);
        oriented = oriented * flipQ;

        // Convertemos para Euler apenas para aplicar sensibilidade e clamp simples
        Vector3 e = oriented.eulerAngles;
        e.x = (e.x > 180f) ? e.x - 360f : e.x;
        e.y = (e.y > 180f) ? e.y - 360f : e.y;
        e.z = (e.z > 180f) ? e.z - 360f : e.z;

        // aplica sensibilidade (escala angular)
        e *= rotationSensitivity;
        e.x = Mathf.Clamp(e.x, -180f, 180f);
        e.y = Mathf.Clamp(e.y, -180f, 180f);
        e.z = Mathf.Clamp(e.z, -180f, 180f);

        // candidate target
        Quaternion candidateTarget = Quaternion.Euler(e);

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

        // aplica smoothing adaptado ao deltaTime
        // MOD TROQUEI PRO FIXEDUPDATE
        /*if (applyRotation)
        {
            if (rotationSmoothing <= 0f || firstFrame)
            {
                transform.rotation = target;
            }
            else
            {
                float dt = Mathf.Max(0.0001f, Time.deltaTime);
                float t = 1f - Mathf.Pow(1f - rotationSmoothing, dt * 60f);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
            }
        }*/
        targetRotation = target;

        //MOD fazer ela seguir o pai
        transform.position = (transform.parent.position);
        //Debug.Log(swordRb.position);
        //Debug.Log(transform.position);

        // OBS: NÃO setamos firstFrame=false aqui — vamos fazer na FixedUpdate
        //firstFrame = false;
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

        // MOD: após a primeira aplicação em FixedUpdate, marcamos que não é mais o primeiro frame
        if (firstFrame) firstFrame = false;
    }
    
    void OnCollisionEnter(Collision col)
    {
        if (col == null) return;
        if (joycons == null || joycons.Count <= jcIndex) return;

        Joycon joy = joycons[jcIndex];

        Debug.Log("Colidi com: " + col.gameObject.name);

        //Se trigou na hora certa
        if ((joy.GetGyro().magnitude >= velocidadeParaTrigger) && col.gameObject.layer == LayerMask.NameToLayer("Corte"))
        {
            
            //Testa se vai dar bom.
            int index = Random.Range(0, sides.Count);
            int result = sides[index];
            Debug.Log($"🎲 Rolou: {result}");

            //Se deu certo então corta
            if (result == 1)
            {
                Debug.Log("✅ Deu 1! TRUE");

                Debug.Log("Trigou");
                joy.SetRumble(80, 160, 0.6f, 100);
                cutter.Cut();

                sides = new List<int> { 1, 2, 3, 4, 5, 6 };

            }
            else //Se rodou diferente de um então só empurra
            {
                joy.SetRumble(80, 160, 0.6f, 50);
                Debug.Log("❌ Não deu 1. FALSE — removendo esse número...");
                sides.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Calibração "centralizante": alinha a espada para a orientação da calibrationReference.
    /// Aplicação IMEDIATA: ajusta transform.rotation, previousTarget e firstFrame para evitar smoothing/deadzone atrapalhando.
    /// </summary>
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

    /// <summary>
    /// Recalibração rápida: redefine o offset de forma que o raw atual mapeie para a rotação atual do objeto (sem pular visualmente).
    /// </summary>
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
