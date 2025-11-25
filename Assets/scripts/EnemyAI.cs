using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    [SerializeField] public Transform currentTarget;
    public EngagementTracker targetEngagement;
    public EngagementTracker myEngagementTracker;
    public Collider attackCollider;
    public VariavelGlobal variaveisGlobais;
    
    public Transform playerTarget;
    public EngagementTracker playerEngagement;
    
    [Header("Detecção e Alvos")]
    public float detectionRadius = 15f;
    public LayerMask targetLayerMask;
    
    [Header("Configurações de Combate")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackRate = 1.5f;
    public float nextAttackTime = 0f;

    public Animator animator;
    public static readonly int SpeedHash = Animator.StringToHash("Speed");
    public static readonly int AttackHash = Animator.StringToHash("Attack");
    public static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    public static readonly int InRangeHash = Animator.StringToHash("inRange");

    public ZombieSpawner spawner;

    public bool tahAtacando = false;

    private Coroutine attackWatchdogCoroutine;
    public float maxAttackStallTime = 3.5f; // watchdog: tempo máximo permitido em tahAtacando

    void Awake()
    {
        //NavMeshHit hit;

        if (!TryGetComponent(out agent))
        {
            Debug.LogError("EnemyAI requer um NavMeshAgent! Adicione o componente ao GameObject: " + gameObject.name);
            enabled = false;
            return;
        }

        if (!TryGetComponent(out myEngagementTracker))
        {
            Debug.LogError("EnemyAI requer o EngagementTracker para funcionar.");
            enabled = false;
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
            playerEngagement = playerTarget.GetComponent<EngagementTracker>();

            if (playerEngagement == null)
            {
                Debug.LogWarning("O Player precisa do componente EngagementTracker para ser perseguido!");
            }
        }
        else
        {
            Debug.LogError("O GameObject com a tag 'Player' não foi encontrado na cena!");
        }

        agent.stoppingDistance = attackRange;

        /*if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning(
                $"🚨 O GameObject '{gameObject.name}' não consegue encontrar o NavMesh. " +
                "Certifique-se de que o chão da cena foi 'cozido' (Baked) corretamente para o NavMesh.");
        }*/
    }

    void Start()
    {
        animator = transform.gameObject.GetComponent<Animator>();
        spawner = FindFirstObjectByType<ZombieSpawner>();

        if (variaveisGlobais == null)
        {
            variaveisGlobais = FindFirstObjectByType<VariavelGlobal>();
            if (variaveisGlobais == null)
                Debug.LogWarning($"[EnemyAI] VariavelGlobal não encontrada para o inimigo '{name}'. Atribua via Inspector ou use Singleton.");
        }
    }

    void Update()
    {
        FindNewTarget();

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);

            //Avisa a velocidade do divo
            float speed = agent.velocity.magnitude;
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= attackRange + 0.1f)
            {
                animator.SetBool(InRangeHash, true);
                if(Time.time >= nextAttackTime)
                {
                    AttackTarget();
                    nextAttackTime = Time.time + attackRate;
                }
                
            }
            else
            {
                animator.SetBool(InRangeHash, false);
                animator.SetFloat(SpeedHash, speed);
            }
        }
        else
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    void FindNewTarget()
    {
        /*
        if (myEngagementTracker.IsTargeted && myEngagementTracker.CurrentTracker != null)
        {
            Transform retaliator = myEngagementTracker.CurrentTracker;
            EngagementTracker retaliatorStatus = retaliator.GetComponent<EngagementTracker>();
            
            myEngagementTracker.StartTracking(retaliator);
            
            if (currentTarget != retaliator)
            {
                if (currentTarget != null && targetEngagement != null)
                {
                    targetEngagement.StopTracking(this.transform);
                }

                currentTarget = retaliator;
                targetEngagement = retaliatorStatus;
            }
            return; 
        }
        
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, targetLayerMask);
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        EngagementTracker bestTargetStatus = null;
        
        if (currentTarget != null)
        {
            if (!IsTargetInSphere(currentTarget, targets))
            {
                if (targetEngagement != null)
                {
                    targetEngagement.StopTracking(this.transform);
                }
                currentTarget = null;
                targetEngagement = null;
            }
            else
            {
                bestTarget = currentTarget;
                bestTargetStatus = targetEngagement;
                closestDistance = Vector3.Distance(transform.position, currentTarget.position); 
            }
        }
        
        foreach (Collider target in targets)
        {
            if (target.transform == transform || target.transform == currentTarget) 
                continue; 

            EngagementTracker targetStatus = target.GetComponent<EngagementTracker>();
            if (targetStatus == null)
                continue;

            if (targetStatus.IsTargeted)
            {
                continue; 
            }
            
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target.transform;
                bestTargetStatus = targetStatus;
            }
        }
        
        if (currentTarget != bestTarget)
        {
            if (currentTarget != null && targetEngagement != null && targetEngagement.CurrentTracker == this.transform)
            {
                targetEngagement.StopTracking(this.transform);
            }
            
            currentTarget = bestTarget;
            targetEngagement = bestTargetStatus;

            if (currentTarget != null && targetEngagement != null)
            {
                targetEngagement.StartTracking(this.transform);
            }
        }*/
        
        if (currentTarget == null && playerTarget != null && playerEngagement != null)
        {
            //if (!playerEngagement.IsTargeted || playerEngagement.CurrentTracker == this.transform)
            //{
                currentTarget = playerTarget;
                targetEngagement = playerEngagement;
                //playerEngagement.StartTracking(this.transform);
            //}
        }
    }
    
    void AttackTarget()
    {
        if (currentTarget == null) return;
        if (tahAtacando == true) return;
        
        //freeze navmesh movement/rotation e zera velocidade
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat(SpeedHash, 0f);

        agent.updatePosition = false;
        agent.updateRotation = false;

        //garantir que o zumbi olhe para o alvo (suave)
        Vector3 lookPos = currentTarget.position;
        lookPos.y = transform.position.y;
        Quaternion wanted = Quaternion.LookRotation((lookPos - transform.position).normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, 1f); 

        // dispara animação
        tahAtacando = true;
        int index = Random.Range(1, 3);
        animator.SetFloat(AttackIndexHash, index);
        animator.SetTrigger(AttackHash);

        if (attackWatchdogCoroutine != null) StopCoroutine(attackWatchdogCoroutine);
        attackWatchdogCoroutine = StartCoroutine(AttackWatchdog());
    }

    public void OnAttackAnimationEnd()
    {
        tahAtacando = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;

        if (attackWatchdogCoroutine != null)
        {
            StopCoroutine(attackWatchdogCoroutine);
            attackWatchdogCoroutine = null;
        }

        //animator.SetTrigger(AttackHash);
        //animator.SetFloat(SpeedHash, 1);
    }

    public void StartCollisionAttack()
    {
        attackCollider.enabled = true;
    }

    public void EndCollisionAttack()
    {
        attackCollider.enabled = false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (attackCollider.enabled == true)
        {
            Debug.Log("Collidi com" + other.gameObject.name);

            //Se for o player e tiver a vida então tira vida
            if (other.gameObject.GetComponent<Health>() != null)
            {
                //UnityEngine.Debug.Log("matou o veio");
                other.gameObject.GetComponent<Health>().TakeDamage(attackDamage);
                variaveisGlobais.StartTomouDano();
            }
        }
    }

    IEnumerator AttackWatchdog()
    {
        float started = Time.time;
        while (tahAtacando && Time.time - started < maxAttackStallTime)
        {
            yield return null;
        }

        if (tahAtacando)
        {
            Debug.LogWarning($"[EnemyWatchdog:{name}] tahAtacando ficou true por {Time.time - started:F2}s -> forçando restauracao.");
            // tenta restaurar
            OnAttackAnimationEnd();
        }
        attackWatchdogCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    public void die()
    { 
        playerEngagement.StopTracking(this.transform);
        spawner.NotifyZombieRemoved();
    }

    void OnDestroy()
    {
        if (Application.isPlaying && spawner != null) die();
    }

}