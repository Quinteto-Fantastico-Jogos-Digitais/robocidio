using System.Collections;
using UnityEngine;

public class EnemyTwinSword : MonoBehaviour
{
    [Header("References")]
    public Transform leftSword;              // transform visual da espada esquerda
    public Transform rightSword;             // transform visual da espada direita
    public Transform player;                 // referência ao jogador

    [Header("Optional - to avoid player's swords")]
    public Transform playerLeftSword;        // opcional: referência às espadas do jogador
    public Transform playerRightSword;

    [Header("Idle / Joy-Con feel")]
    public Vector3 swordModelOffsetEuler = new Vector3(0, 0, 0); // ajuste conforme a orientação do modelo
    public float idleSwayAmplitude = 6f;     // graus de oscilação típica
    public float idleSwaySpeed = 1.2f;       // velocidade do sway
    public float noiseStrength = 2f;         // ruído extra (graus)
    public float rotationSmoothing = 8f;     // maior = mais suave

    [Header("Attack")]
    public float attackInterval = 2.0f;      // tempo entre tentativas de ataque
    public float attackDuration = 0.28f;     // duração do swing
    public float attackWindup = 0.12f;       // preparação antes do swing
    public float avoidAngleDeg = 35f;        // se vetor ao player's sword estiver muito perto, desvia
    public float lateralOffset = 0.5f;       // deslocamento lateral do alvo do ataque (evita acertar as espadas)
    public float swingExtraAngle = 45f;      // arco extra aplicado durante o swing

    // interno
    float attackTimer = 0f;
    bool isAttacking = false;
    Quaternion leftRestRot, rightRestRot; // armazenam orientação base
    float seed;

    void Start()
    {
        seed = Random.value * 100f;
        if (leftSword == null || rightSword == null || player == null)
            Debug.LogWarning("EnemyTwinSwordAI: set leftSword, rightSword and player in inspector.");

        // calcula rotações base (aplica offset do modelo)
        if (leftSword != null) leftRestRot = leftSword.localRotation * Quaternion.Euler(swordModelOffsetEuler);
        if (rightSword != null) rightRestRot = rightSword.localRotation * Quaternion.Euler(swordModelOffsetEuler);
        attackTimer = Random.Range(0f, attackInterval * 0.6f); // dessincroniza múltiplos inimigos
    }

    void Update()
    {
        if (leftSword == null || rightSword == null || player == null) return;

        attackTimer += Time.deltaTime;
        if (!isAttacking && attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartCoroutine(DoAttackSequence());
        }

        if (!isAttacking)
        {
            // idle movement (joy-con like): sway + perlin noise + smoothing
            ApplyIdleMotion(leftSword, leftRestRot, 0f);
            ApplyIdleMotion(rightSword, rightRestRot, 180f); // pequeno offset de fase para diferença entre espadas
        }
    }

    void ApplyIdleMotion(Transform sword, Quaternion restRot, float phaseOffset)
    {
        float t = Time.time * idleSwaySpeed + phaseOffset + seed;
        float sway = Mathf.Sin(t) * idleSwayAmplitude;                  // base sinusoidal
        float noise = (Mathf.PerlinNoise(t * 0.5f, seed) - 0.5f) * 2f * noiseStrength;
        Quaternion target = restRot * Quaternion.Euler(sway + noise, noise * 0.5f, noise * 0.25f);
        sword.localRotation = Quaternion.Slerp(sword.localRotation, target, Time.deltaTime * rotationSmoothing);
    }

    IEnumerator DoAttackSequence()
    {
        isAttacking = true;

        // 1) windup: move as espadas para posição de preparação (posição ligeiramente levantada)
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 attackDir = ChooseAttackDirectionAvoidingPlayerSwords(toPlayer);

        // calculamos rotações alvo para a preparação (só orientações, não posições)
        Quaternion prepLeft = Quaternion.LookRotation(attackDir, Vector3.up) * Quaternion.Euler(0, -25f, 90f) * Quaternion.Euler(swordModelOffsetEuler);
        Quaternion prepRight = Quaternion.LookRotation(attackDir, Vector3.up) * Quaternion.Euler(0, 25f, -90f) * Quaternion.Euler(swordModelOffsetEuler);

        float t = 0f;
        while (t < attackWindup)
        {
            float lerp = Mathf.SmoothStep(0f, 1f, t / attackWindup);
            leftSword.localRotation = Quaternion.Slerp(leftSword.localRotation, prepLeft, lerp);
            rightSword.localRotation = Quaternion.Slerp(rightSword.localRotation, prepRight, lerp);
            t += Time.deltaTime;
            yield return null;
        }

        // 2) swing: animação simples que gira as espadas pelo arco (aplica swingExtraAngle)
        Quaternion swingLeftEnd = prepLeft * Quaternion.Euler(-swingExtraAngle, 0, 0);
        Quaternion swingRightEnd = prepRight * Quaternion.Euler(-swingExtraAngle, 0, 0);

        t = 0f;
        while (t < attackDuration)
        {
            float lerp = Mathf.SmoothStep(0f, 1f, t / attackDuration);
            leftSword.localRotation = Quaternion.Slerp(prepLeft, swingLeftEnd, lerp);
            rightSword.localRotation = Quaternion.Slerp(prepRight, swingRightEnd, lerp);

            // opcional: aqui você poderia disparar detecção de hit (raycast/Overlap) durante o meio do swing
            // if (Mathf.Abs(lerp - 0.5f) < 0.1f) CheckHit();

            t += Time.deltaTime;
            yield return null;
        }

        // 3) retorno suave às posições de descanso
        t = 0f;
        float returnDuration = 0.18f;
        Quaternion curLeft = leftSword.localRotation;
        Quaternion curRight = rightSword.localRotation;
        while (t < returnDuration)
        {
            float lerp = Mathf.SmoothStep(0f, 1f, t / returnDuration);
            leftSword.localRotation = Quaternion.Slerp(curLeft, leftRestRot, lerp);
            rightSword.localRotation = Quaternion.Slerp(curRight, rightRestRot, lerp);
            t += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    // seleciona direção de ataque e, se estiver muito perto das swords do jogador, escolhe um desvio lateral
    Vector3 ChooseAttackDirectionAvoidingPlayerSwords(Vector3 baseDir)
    {
        Vector3 dir = baseDir.normalized;

        // se não há referência às espadas do jogador, talvez deslocamos levemente para o centro do corpo
        if (playerLeftSword == null || playerRightSword == null)
            return dir;

        // calcula ângulo entre dir e vetores para cada espada do jogador
        Vector3 toLeftSword = (playerLeftSword.position - transform.position).normalized;
        Vector3 toRightSword = (playerRightSword.position - transform.position).normalized;

        float angLeft = Vector3.Angle(dir, toLeftSword);
        float angRight = Vector3.Angle(dir, toRightSword);

        // se algum está muito perto do nosso ataque, escolhe um desvio lateral do outro lado
        if (angLeft < avoidAngleDeg || angRight < avoidAngleDeg)
        {
            // escolhe perpendicular (em torno do up) com sinal para afastar do lado com menor ângulo
            Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized; // perp to dir
            // decide direcao de desvio: se esquerda está mais próxima, desvia para o lado oposto
            float sign = (angLeft < angRight) ? 1f : -1f;
            Vector3 deviated = (dir + perp * sign * lateralOffset).normalized;
            return deviated;
        }

        return dir;
    }

    // opcional: desenha gizmo para debug do ataque
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (player.position - transform.position).normalized * 1.2f);
        }
        if (leftSword != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(leftSword.position, leftSword.forward * 0.5f);
        }
        if (rightSword != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(rightSword.position, rightSword.forward * 0.5f);
        }
    }
}
