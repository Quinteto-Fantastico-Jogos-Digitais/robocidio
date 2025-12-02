using System;
using UnityEngine;
using DynamicMeshCutter;

/// <summary>
/// Full Sword controller (handle-pivot variant) - robust, kinematic (cut-only)
/// - Sword_Root follows handTransform (position)
/// - handlePivot (child) is rotated to aim; Mesh must be child of handlePivot
/// - Tip must be child of Sword_Root (NOT of handlePivot)
/// - Virtual cursor supports Cursor.lockState
/// - Local-space clamps, wrap-avoidance (±360 test), speed-limit, smoothing
/// - Use kinematic Rigidbody + trigger colliders for cutting
/// </summary>
[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform handTransform;    // hand position the root will follow
    public Transform handlePivot;      // pivot at the grip (mesh sits under this)
    public Transform tip;              // tip (must be child of Sword_Root, not of handlePivot)
    public Transform playerBody;       // optional: used to evaluate local-space clamps
    public Camera cam;                 // optional, defaults to Camera.main
    public PlaneBehaviour cutter;

    public VariavelGlobal variaveisGlobais;

    [Header("Control")]
    [Tooltip("0 = left mouse, 1 = right mouse")]
    public int controlMouseButton = 1;

    [Header("Position follow")]
    public Vector3 followPositionOffset = Vector3.zero;
    [Range(0f, 0.99f)]
    public float positionSmoothing = 0.85f;

    [Header("Virtual cursor")]
    [Tooltip("Distance from camera used to place the invisible mouse target")]
    public float mouseFollowerDistance = 3f;
    public bool clampToScreen = true;
    public Vector2 screenMargin = new Vector2(4f, 4f);

    [Header("Rotation")]
    public float rotationSensitivity = 1f;
    [Range(0f, 0.99f)]
    public float rotationSmoothing = 0.9f;

    [Header("Local limits (player local-space)")]
    public float limitPitchMin = -50f; // X
    public float limitPitchMax = 50f;
    public float limitYawMin = -100f;  // Y
    public float limitYawMax = 100f;

    [Header("Misc safety")]
    [Tooltip("Max local degrees per second change allowed (0 = off)")]
    public float maxLocalDegPerSecond = 720f;

    [Header("Model compensation")]
    [Tooltip("If your mesh was imported rotated relative to +Z forward, apply compensation (deg)")]
    public Vector3 modelRotationOffsetEuler = Vector3.zero;

    [Header("Debug")]
    public bool debugDrawGizmos = false;

    // small capture-edge tracking
    private bool prevRightCapture = false;
    private bool prevLeftCapture = false;
    private bool anchorAtTip = false;

    // internals
    Rigidbody rb;
    private Quaternion simulatedQuat;       // for mouse-simulated orientation
    private Quaternion previousLocal = Quaternion.identity;
    private Quaternion targetWorld = Quaternion.identity;

    // virtual cursor shared
    private static Vector2 sharedVirtualMousePos = Vector2.zero;
    private static int lastVirtualUpdateFrame = -1;
    private static float virtualMouseSpeed = 10f; // tuning when locked

    private Vector3 virtualMousePos;
    private float lastMouseMoveTime = -999f;
    public float mouseActiveTimeout = 0.25f;

    // cached targets for physics step
    private Vector3 _desiredPosition;
    private Quaternion _targetWorldRotation;
    private bool firstFrame = true;

    // globals for capture state
    public static bool IsMouseCapturing = false;
    public static int MouseCapturingIndex = -1; // -1 none, 0 left,1 right

    // fallback angular speed
    private const float fallbackMouseAngSpeed = 180f;

    public SomController som;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        // ensure kinematic mode for cut-only usage
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SwordMouseController_HandlePivot_Full] For cutting, Rigidbody.isKinematic is recommended. Forcing isKinematic=true on '{name}'.");
#endif
                rb.isKinematic = true;
            }

            // check collider trigger suggestion
            Collider c = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (c != null && !c.isTrigger)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SwordMouseController_HandlePivot_Full] Collider on '{name}' is not a trigger. For cutting prefer Collider.isTrigger = true.");
#endif
            }
        }

        // ensure handlePivot exists; if not, try to find a child named "handlePivot" or create one
        if (handlePivot == null)
        {
            Transform found = transform.Find("handlePivot");
            if (found != null) handlePivot = found;
            else
            {
                GameObject go = new GameObject("handlePivot");
                handlePivot = go.transform;
                handlePivot.SetParent(transform, worldPositionStays: true);
                // place at hand transform if available, else at root
                if (handTransform != null)
                {
                    handlePivot.position = handTransform.position;
                    handlePivot.rotation = handTransform.rotation;
                }
                else
                {
                    handlePivot.position = transform.position;
                    handlePivot.rotation = transform.rotation;
                }

                // attempt to reparent first Mesh child under the pivot to follow convention
                Transform meshChild = null;
                foreach (Transform t in transform)
                {
                    if (t == handlePivot) continue;
                    if (t.GetComponent<MeshRenderer>() != null || t.GetComponent<SkinnedMeshRenderer>() != null)
                    {
                        meshChild = t;
                        break;
                    }
                }
                if (meshChild != null)
                    meshChild.SetParent(handlePivot, worldPositionStays: true);
            }
        }

        // initialize simulatedQuat from real orientation
        simulatedQuat = handlePivot != null ? handlePivot.rotation : transform.rotation;

        // init virtual mouse center
        if (sharedVirtualMousePos == Vector2.zero)
            sharedVirtualMousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        virtualMousePos = sharedVirtualMousePos;

        // init previousLocal based on handlePivot orientation relative to player
        if (playerBody != null)
            previousLocal = Quaternion.Inverse(playerBody.rotation) * (handlePivot != null ? handlePivot.rotation : transform.rotation);
        else
            previousLocal = handlePivot != null ? handlePivot.rotation : transform.rotation;
    }

    void Update()
    {
        UpdateVirtualMouse();

        // update global capture flags
        bool right = Input.GetMouseButton(1);
        bool left = Input.GetMouseButton(0);
        IsMouseCapturing = right || left;
        if (right && !left) MouseCapturingIndex = 1;
        else if (left && !right) MouseCapturingIndex = 0;
        else MouseCapturingIndex = -1;

        // update last mouse move time
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        if (Mathf.Abs(mouseDelta.x) > 0f || Mathf.Abs(mouseDelta.y) > 0f)
            lastMouseMoveTime = Time.time;

        bool capturingThis = Input.GetMouseButton(controlMouseButton);

        // --- SYNCHRONIZE SIMULATION ON CAPTURE START TO AVOID JUMPS ---
        // detect edge: started capturing this frame?
        bool wasCapturingLastFrame = (controlMouseButton == 1) ? prevRightCapture : prevLeftCapture; 
        // (we'll maintain prevRightCapture/prevLeftCapture as small state flags)

        /*if (capturingThis && !wasCapturingLastFrame)
        {
            // sync simulatedQuat with current visual rotation to avoid discontinuity
            if (handlePivot != null)
                simulatedQuat = handlePivot.rotation;
            else
                simulatedQuat = transform.rotation;

            // update previousLocal so candidate selection won't jump
            if (playerBody != null)
                previousLocal = Quaternion.Inverse(playerBody.rotation) * simulatedQuat;
            else
                previousLocal = simulatedQuat;
        }*/
        if (capturingThis && !wasCapturingLastFrame)
        {
            // sync simulatedQuat with current visual rotation to avoid discontinuity
            if (handlePivot != null)
                simulatedQuat = handlePivot.rotation;
            else
                simulatedQuat = transform.rotation;

            // update previousLocal so candidate selection won't jump
            if (playerBody != null)
                previousLocal = Quaternion.Inverse(playerBody.rotation) * simulatedQuat;
            else
                previousLocal = simulatedQuat;

            // --- NOVO: alinhar o mouse virtual com a direção atual do handlePivot
            // para evitar salto de rotação no momento em que o jogador começa a capturar
            if (cam != null && handlePivot != null)
            {
                // projeta um ponto à frente do pivot na distância mouseFollowerDistance
                Vector3 forwardPoint = handlePivot.position + handlePivot.forward * mouseFollowerDistance;
                Vector3 fpScreen = cam.WorldToScreenPoint(forwardPoint);

                sharedVirtualMousePos = new Vector2(
                    Mathf.Clamp(fpScreen.x, screenMargin.x, Screen.width - screenMargin.x),
                    Mathf.Clamp(fpScreen.y, screenMargin.y, Screen.height - screenMargin.y)
                );
                virtualMousePos = sharedVirtualMousePos;

                // opcional: se você usa anchorAtTip, também pode manter anchorTipDepth aqui:
                // anchorTipDepth = fpScreen.z;
            }
        }

        // save capture state for next frame
        if (controlMouseButton == 1)
            prevRightCapture = capturingThis;
        else
            prevLeftCapture = capturingThis;


        // compute rotation target for handlePivot (world-space)
        Quaternion computedWorld = simulatedQuat;

        if (capturingThis)
        {
            if (cam != null && tip != null && handlePivot != null)
            {
                Vector3 mpos = virtualMousePos;
                if (clampToScreen)
                {
                    mpos.x = Mathf.Clamp(mpos.x, screenMargin.x, Screen.width - screenMargin.x);
                    mpos.y = Mathf.Clamp(mpos.y, screenMargin.y, Screen.height - screenMargin.y);
                }

                Vector3 screenPoint = new Vector3(mpos.x, mpos.y, mouseFollowerDistance);
                Vector3 worldTarget = cam.ScreenToWorldPoint(screenPoint);

                //CHANGE
                Vector3 basePos = handlePivot.position; // rotate around pivot
                //Vector3 basePos = (anchorAtTip && tip != null) ? tip.position : (handlePivot != null ? handlePivot.position : transform.position);
                Vector3 desiredDir = worldTarget - basePos;

                if (desiredDir.sqrMagnitude > 0.000001f)
                {
                    Vector3 upRef = (playerBody != null) ? playerBody.up : cam.transform.up;
                    Quaternion desiredWorld = Quaternion.LookRotation(desiredDir.normalized, upRef);

                    // compensate model rotation if mesh import axes differ
                    desiredWorld = desiredWorld * Quaternion.Euler(modelRotationOffsetEuler);

                    // convert to local-space to clamp
                    Quaternion desiredLocal = (playerBody != null) ? Quaternion.Inverse(playerBody.rotation) * desiredWorld : desiredWorld;
                    Vector3 le = desiredLocal.eulerAngles;
                    le.x = (le.x > 180f) ? le.x - 360f : le.x;
                    le.y = (le.y > 180f) ? le.y - 360f : le.y;
                    le.z = (le.z > 180f) ? le.z - 360f : le.z;

                    // apply sensitivity + clamps
                    le *= rotationSensitivity;
                    le.x = Mathf.Clamp(le.x, -180f, 180f);
                    le.y = Mathf.Clamp(le.y, -180f, 180f);
                    le.z = Mathf.Clamp(le.z, -180f, 180f);

                    // find variant (±360 shifts) closest to previousLocal to avoid jumps
                    Quaternion baseCandidateLocal = Quaternion.Euler(le);
                    Quaternion bestCandidateLocal = baseCandidateLocal;
                    float bestAngle = Quaternion.Angle(previousLocal, baseCandidateLocal);
                    float[] shifts = new float[] { -360f, 0f, 360f };
                    for (int ix = 0; ix < 3; ix++)
                        for (int iy = 0; iy < 3; iy++)
                            for (int iz = 0; iz < 3; iz++)
                            {
                                Vector3 lv = new Vector3(le.x + shifts[ix], le.y + shifts[iy], le.z + shifts[iz]);
                                Quaternion qLocal = Quaternion.Euler(lv);
                                float ang = Quaternion.Angle(previousLocal, qLocal);
                                if (ang < bestAngle) { bestAngle = ang; bestCandidateLocal = qLocal; }
                            }

                    Quaternion candidateLocal = bestCandidateLocal;

                    // deadzone in local-space (keep previous if tiny change)
                    Quaternion targetLocal;
                    if (!firstFrame)
                    {
                        float angleDiffLocal = Quaternion.Angle(previousLocal, candidateLocal);
                        if (angleDiffLocal < Mathf.Max(0.0001f, 0f))
                            targetLocal = previousLocal;
                        else
                            targetLocal = candidateLocal;
                    }
                    else
                    {
                        targetLocal = candidateLocal;
                    }

                    // limit angular speed (local) per frame
                    if (!firstFrame && maxLocalDegPerSecond > 0f)
                    {
                        float angNow = Quaternion.Angle(previousLocal, targetLocal);
                        float maxDelta = maxLocalDegPerSecond * Time.deltaTime;
                        if (angNow > maxDelta && angNow > 0.0001f)
                        {
                            float frac = maxDelta / angNow;
                            targetLocal = Quaternion.Slerp(previousLocal, targetLocal, frac);
                        }
                    }

                    previousLocal = targetLocal;
                    computedWorld = (playerBody != null) ? playerBody.rotation * targetLocal : targetLocal;
                }
            }
            else
            {
                // fallback: small delta integration to simulatedQuat
                float dt = Mathf.Max(0.0001f, Time.deltaTime);
                float yawDeg = Input.GetAxisRaw("Mouse X") * fallbackMouseAngSpeed * dt;
                float pitchDeg = -Input.GetAxisRaw("Mouse Y") * fallbackMouseAngSpeed * dt;
                Quaternion delta = Quaternion.Euler(pitchDeg, yawDeg, 0f);
                simulatedQuat = delta * simulatedQuat;
                computedWorld = simulatedQuat;
            }
        }
        else if ((Time.time - lastMouseMoveTime) <= mouseActiveTimeout)
        {
            // mouse recently active: keep simulated
            computedWorld = simulatedQuat;
        }
        else
        {
            computedWorld = simulatedQuat;
        }

        if (!capturingThis)
        {
            // targetLocal = identity (0,0,0)
            Quaternion localZero = Quaternion.identity;

            // convert to world-space if we have playerBody
            if (playerBody != null)
                computedWorld = playerBody.rotation * localZero;
            else
                computedWorld = localZero;

            //NOVO
            anchorAtTip = false;
        }

        // cache position & rotation for FixedUpdate
        //_desiredPosition = (handTransform != null) ? (handTransform.position + handTransform.TransformVector(followPositionOffset)) : transform.position;
        _desiredPosition = (handTransform != null) ? handTransform.TransformPoint(followPositionOffset) : transform.position;
        _targetWorldRotation = computedWorld;

        //CHANGE: Mover a espada com o corpo
        /*if (transform.parent != null)
            transform.position = transform.parent.position;
        else
            transform.position = transform.position;*/ // sem alteração
        if (positionSmoothing <= 0f || firstFrame)
            transform.position = _desiredPosition;
        else
            //transform.position = transform.parent.position;
            transform.position = Vector3.Lerp(transform.position, _desiredPosition, 1f - positionSmoothing);
        if (handlePivot != null)
        {
            if (rotationSmoothing <= 0f || firstFrame)
                handlePivot.rotation = _targetWorldRotation;
            else
                handlePivot.rotation = Quaternion.Slerp(handlePivot.rotation, _targetWorldRotation, 1f - rotationSmoothing);
        }
        else
        {
            if (rotationSmoothing <= 0f || firstFrame)
                transform.rotation = _targetWorldRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetWorldRotation, 1f - rotationSmoothing);
        }
    }

    void FixedUpdate()
    {
        // position: move the root to follow hand (kinematic style)
        //CHANGE: MUDEI PRO UPDATE
        /*if (positionSmoothing <= 0f || firstFrame)
            transform.position = _desiredPosition;
        else
            transform.position = Vector3.Lerp(transform.position, _desiredPosition, 1f - positionSmoothing);*/

        // rotation: apply rotation to handlePivot only (so handle remains fixed at hand)
        /*if (handlePivot != null)
        {
            if (rotationSmoothing <= 0f || firstFrame)
                handlePivot.rotation = _targetWorldRotation;
            else
                handlePivot.rotation = Quaternion.Slerp(handlePivot.rotation, _targetWorldRotation, 1f - rotationSmoothing);
        }
        else
        {
            if (rotationSmoothing <= 0f || firstFrame)
                transform.rotation = _targetWorldRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetWorldRotation, 1f - rotationSmoothing);
        }*/

        if (firstFrame) firstFrame = false;
    }

    void UpdateVirtualMouse()
    {
        if (lastVirtualUpdateFrame == Time.frameCount)
        {
            virtualMousePos = sharedVirtualMousePos;
            return;
        }

        lastVirtualUpdateFrame = Time.frameCount;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            sharedVirtualMousePos += delta * virtualMouseSpeed * Mathf.Max(0.0001f, Time.deltaTime) * 100f;
        }
        else
        {
            sharedVirtualMousePos = Input.mousePosition;
        }

        sharedVirtualMousePos.x = Mathf.Clamp(sharedVirtualMousePos.x, screenMargin.x, Screen.width - screenMargin.x);
        sharedVirtualMousePos.y = Mathf.Clamp(sharedVirtualMousePos.y, screenMargin.y, Screen.height - screenMargin.y);

        virtualMousePos = sharedVirtualMousePos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!this.enabled) return;
        if (!IsMouseCapturing) return;

        Debug.Log("Colidi com: " + other.gameObject.name);
        cutter.Cut();

        //Se for inimigo chama a função de morrer
        if (other.gameObject.GetComponentInParent<EnemyAI>() != null)
        {
            //UnityEngine.Debug.Log("matou o veio");
            variaveisGlobais.SomaPontos(130);
            other.gameObject.GetComponentInParent<EnemyAI>().die();
            som.PlayEspadaAndWait(UnityEngine.Random.Range(0, 2));
        }

    }

    void OnDrawGizmosSelected()
    {
        if (!debugDrawGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(_desiredPosition, 0.03f);
        if (cam != null)
        {
            Vector3 spt = new Vector3(sharedVirtualMousePos.x, sharedVirtualMousePos.y, mouseFollowerDistance);
            Vector3 wt = cam.ScreenToWorldPoint(spt);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(wt, 0.04f);
            Gizmos.DrawLine(_desiredPosition, wt);
        }

        if (handTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(handTransform.position, handTransform.position + handTransform.forward * 1f);
            Gizmos.DrawSphere(handTransform.TransformPoint(followPositionOffset), 0.06f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(handTransform.position, _desiredPosition);
            Gizmos.DrawSphere(_desiredPosition, 0.04f);
        }

        if (playerBody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerBody.position, playerBody.position + playerBody.forward * 1f);
        }
    }
}
