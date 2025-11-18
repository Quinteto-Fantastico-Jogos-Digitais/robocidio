using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] private Transform currentTarget;
    private EngagementTracker targetEngagement;
    private EngagementTracker myEngagementTracker;
    
    private Transform playerTarget;
    private EngagementTracker playerEngagement;
    
    [Header("Detecção e Alvos")]
    public float detectionRadius = 15f;
    public LayerMask targetLayerMask;
    
    [Header("Configurações de Combate")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackRate = 1.5f;
    private float nextAttackTime = 0f;

    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int InRangeHash = Animator.StringToHash("inRange");

    private ZombieSpawner spawner;

    private Coroutine attackCoroutine;

    public bool tahAtacando = false;

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
                    //Debug.Log("to em ti cuzão");
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
    
    /*private bool IsTargetInSphere(Transform targetTransform, Collider[] targets)
    {
        foreach (Collider collider in targets)
        {
            if (collider.transform == targetTransform)
            {
                return true;
            }
        }
        return false;
    }*/
    
    void AttackTarget()
    {
        if (currentTarget == null) return;
        if (tahAtacando == true) return;
        
        //freeze navmesh movement/rotation e zera velocidade
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat(SpeedHash, 0);

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
        
        //Health targetHealth = currentTarget.GetComponent<Health>();
        
        /*if (targetHealth != null)
        {
            targetHealth.TakeDamage(attackDamage); 
        }*/
    }

    /*void AttackTarget()
    {
        if (attackCoroutine != null) return; // evita double attack coroutines
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        if (currentTarget == null) { attackCoroutine = null; yield break; }

        //freeze navmesh movement/rotation e zera velocidade
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        agent.updatePosition = false;
        agent.updateRotation = false;

        //garantir que o zumbi olhe para o alvo (suave)
        Vector3 lookPos = currentTarget.position;
        lookPos.y = transform.position.y;
        Quaternion wanted = Quaternion.LookRotation((lookPos - transform.position).normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, 1f); 

        // dispara animação
        int index = Random.Range(0, 3);
        animator.SetFloat(AttackIndexHash, index);
        animator.SetTrigger(AttackHash);

        // espera até que o estado de ataque termine — método robusto:
        // assume que o estado de ataque tem tag "Attack" ou está em layer 0 com nome "Attack"
        // você pode ajustar o nome do state ou usar Animation Events para detectar fim
        float timeout = 2.5f; // fallback máximo em segundos (ajuste)
        float timer = 0f;
        bool attackStateEntered = false;

        while (timer < timeout)
        {
            timer += Time.deltaTime;
            var st = animator.GetCurrentAnimatorStateInfo(0);
            // ajuste a condição para detectar seu state de attack
            if (st.IsName("Attack") || st.IsTag("Attack"))
            {
                attackStateEntered = true;
                // espera até sair do estado de attack
                while (st.normalizedTime < 1f && timer < timeout)
                {
                    yield return null;
                    timer += Time.deltaTime;
                    st = animator.GetCurrentAnimatorStateInfo(0);
                }
                break;
            }
            yield return null;
        }

        // fallback: aguarda um tempo curto se a animação não foi detectada
        if (!attackStateEntered) yield return new WaitForSeconds(0.5f);

        // restore navmesh control
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;

        attackCoroutine = null;
    }*/

    public void OnAttackAnimationEnd()
    {
        
        tahAtacando = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
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