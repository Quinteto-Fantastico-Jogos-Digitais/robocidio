using UnityEngine;
using UnityEngine.AI;

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

    void Awake()
    {
        NavMeshHit hit;

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

        if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning(
                $"🚨 O GameObject '{gameObject.name}' não consegue encontrar o NavMesh. " +
                "Certifique-se de que o chão da cena foi 'cozido' (Baked) corretamente para o NavMesh.");
        }
    }

    void Start()
    {
        animator = transform.gameObject.GetComponent<Animator>();
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
                    animator.SetFloat(SpeedHash, 0);
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
            if (!playerEngagement.IsTargeted || playerEngagement.CurrentTracker == this.transform)
            {
                currentTarget = playerTarget;
                targetEngagement = playerEngagement;
                playerEngagement.StartTracking(this.transform);
            }
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
        
        Vector3 lookAtPos = currentTarget.position;
        lookAtPos.y = transform.position.y;
        transform.LookAt(lookAtPos);

        //Trabalha com as animações
        int index = Random.Range(0, 3); // 0,1,2 (int)
        animator.SetFloat(AttackIndexHash, index);
        animator.SetTrigger(AttackHash);
        
        //Health targetHealth = currentTarget.GetComponent<Health>();
        
        /*if (targetHealth != null)
        {
            targetHealth.TakeDamage(attackDamage); 
        }*/
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
    }
}