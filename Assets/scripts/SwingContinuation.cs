using System.Collections;
using UnityEngine;

/// <summary>
/// Detecta swings e força uma continuação procedural do movimento da espada.
/// Integrável com SmoothSwordController: o script apenas calcula e aplica uma "overrideRotation"
/// que o SmoothSwordController pode respeitar (ou você pode deixar este script escrever diretamente no transform).
/// 
/// Principais parâmetros:
/// - swingDuration: quanto tempo dura a continuação
/// - hitWindow: fração do tempo em que os hits estão ativos
/// - blendOut: se true, o input do jogador volta gradualmente ao final do swing
/// </summary>
[DisallowMultipleComponent]
public class SwingContinuation : MonoBehaviour
{
    [Header("Detection")]
    public SwingController swordController; // opcional, para enviar override (se estiver usando)
    public Joycon jc; // optional direct reference if you prefer
    public int jcIndex = 0;
    public float swingGyroThreshold = 350f; // deg/s — ajuste empiricamente
    public float swingVelThreshold = 1.2f;  // m/s (se você calcula sword linear velocity)

    [Header("Swing behavior")]
    public float swingDuration = 0.45f;     // duração total do swing em segundos
    [Range(0f,1f)] public float hitWindowStart = 0.18f; // % do tempo até começar a ser "hit active"
    [Range(0f,1f)] public float hitWindowEnd = 0.5f;    // % do tempo quando termina a janela de hit
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // progress curve
    [Tooltip("Multiplier para o ângulo extra aplicado na direção do swing")]
    public float swingAngleMultiplier = 1.0f;
    [Tooltip("Blend factor: quanto do override (continuação) aplica sobre o input (1 = full override)")]
    [Range(0f,1f)] public float overrideStrength = 0.95f;
    [Tooltip("Se true, o input do jogador é parcialmente respeitado e volta gradualmente.")]
    public bool blendOut = true;

    [Header("Debug / safety")]
    public bool debug = false;

    // internals
    private bool swinging = false;
    private Quaternion startRot;
    private Quaternion targetRot;
    private Vector3 detectedAxis;
    private float detectedAngularSpeed;
    private float swingStartTime;
    private Rigidbody attackerRb;

    // optional: access to SmoothSwordController's internals (if present)
    private SwingController smooth;

    void Start()
    {
        smooth = swordController != null ? swordController.GetComponent<SwingController>() : null;
        if (jc == null && jcIndex >= 0 && JoyconManager.Instance != null)
        {
            var js = JoyconManager.Instance.j;
            if (js != null && js.Count > jcIndex) jc = js[jcIndex];
        }
    }

    void Update()
    {
        if (swinging) return;

        // detect via gyro
        Vector3 gyro = Vector3.zero;
        if (jc != null) gyro = jc.GetGyro();
        float gyroMag = gyro.magnitude;

        // if you have sword linear velocity, check it too (optional)
        // assume swordController might expose sword tip velocity; else rely on gyro
        float swordVelMag = 0f;
        //var swordRb = GetComponent<Rigidbody>();
        //if (swordRb != null) swordVelMag = swordRb.linearVelocity.magnitude;

        
        //Debug.Log("velmag" + swordVelMag);

        if (gyroMag >= swingGyroThreshold || swordVelMag >= swingVelThreshold)
        {
            // determine axis and strength for continuation
            Debug.Log("continuo");
            Debug.Log("gyromag" + gyroMag);
            detectedAxis = gyro.normalized;
            detectedAngularSpeed = gyroMag;
            TriggerSwing();
        }
    }

    void TriggerSwing()
    {
        // capture current rotation as start
        startRot = transform.rotation;

        // compute target rotation: extrapolate along detected axis by an angle proportional to angular speed
        // angle = detectedAngularSpeed * smallFactor * multiplier
        float smallFactor = 15f; // empirically chosen; ajuste se gyro estiver em rad/s em vez de deg/s
        float extraAngle = detectedAngularSpeed * smallFactor * swingAngleMultiplier;
        // clamp to sane range
        extraAngle = Mathf.Clamp(extraAngle, 10f, 110f);

        // build rotation around axis in world space
        targetRot = Quaternion.AngleAxis(extraAngle, detectedAxis) * startRot;

        // start coroutine
        StartCoroutine(ContinueSwingCoroutine());
    }

    IEnumerator ContinueSwingCoroutine()
    {
        swinging = true;
        swingStartTime = Time.time;
        float endTime = swingStartTime + swingDuration;

        // hit enabling flags: here você deve habilitar a detecção de hit do seu Sword (por exemplo, setar sword.canHit = true)
        // We'll raise events by calling a method on sword if present.
        /*var sword = GetComponentInChildren<Sword>(); // supondo que Sword.cs exista
        if (sword != null)
        {
            // disable hits initially — we enable inside window
            sword.enabled = true; // keep script enabled but manage canHit if você implementar
        }*/

        while (Time.time < endTime)
        {
            float t = (Time.time - swingStartTime) / swingDuration;
            float eval = swingCurve.Evaluate(t); // 0..1 along the curve

            // compute interpolation rotation: from start -> target following eval
            Quaternion continuationRot = Quaternion.Slerp(startRot, targetRot, eval);

            // Blend between input rotation and continuation
            // Se você usa SmoothSwordController e quer que ele receba override, chame um método nele; senão aplique diretamente.
            if (smooth != null)
            {

                Debug.Log("CUCUCU");

                // we assume SmoothSwordController exposes a method to accept override rotation (we'll show a small API below)
                float appliedStrength = overrideStrength;
                if (blendOut)
                {
                    // reduce override near the end to allow input regain control
                    appliedStrength *= (1f - t);
                }
                smooth.ApplySwingOverride(continuationRot, appliedStrength);
            }
            else
            {
                // direct write to transform (less preferred; might conflict with smooth controller)
                // blend with current transform.rotation to avoid snapping
                float blend = overrideStrength * (blendOut ? (1f - t) : 1f);
                transform.rotation = Quaternion.Slerp(transform.rotation, continuationRot, blend);
            }

            // manage hit window
            /*if (sword != null)
            {
                float hitStart = hitWindowStart;
                float hitEnd = hitWindowEnd;
                bool hitActive = t >= hitStart && t <= hitEnd;
                // Here we assume Sword has a public property canHit to enable/disable hit detection.
                // If not, você precisa adaptar para sua implementação.
                var swordComp = sword as Sword;
                if (swordComp != null)
                {
                    // precisa implementar canHit no Sword.cs (ex: public bool canHit)
                    swordComp.canHit = hitActive;
                }
            }*/

            yield return null;
        }

        // final cleanup: remove override
        if (smooth != null) smooth.ClearSwingOverride();
        /*if (sword != null)
        {
            var swordComp = sword as Sword;
            if (swordComp != null) swordComp.canHit = false;
        }*/

        swinging = false;
        yield break;
    }
}
