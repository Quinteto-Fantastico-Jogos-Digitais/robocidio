using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] private Transform currentTarget;
    private EngagementTracker targetEngagement;

    [Header("Detecção e Alvos")]
    public float detectionRadius = 15f;
    public LayerMask targetLayerMask;
    
    [Header("Configurações de Combate")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    
    void Awake()
    {
        if (!TryGetComponent(out agent))
        {
            Debug.LogError("EnemyAI requer um NavMeshAgent! Adicione o componente.");
        }
        agent.stoppingDistance = attackRange;
    }

    void Update()
    {
        FindNewTarget();

        // ------------------
        // Lógica de Movimento e Engajamento
        // ------------------
        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            // Logica de Ataque
            if (distanceToTarget <= attackRange + 0.1f)
            {
                AttackTarget(); 
            }
        }
        else
        {
            // Se não há alvo, garante que estamos parados.
            if (agent.hasPath)
            {
                 agent.ResetPath();
            }
            // Não precisamos de StopTracking aqui, pois o FindNewTarget já lida com o desengajamento
        }
    }

    void FindNewTarget()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, targetLayerMask);
        
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        EngagementTracker bestTargetStatus = null;
        
        // --- 1. Determinação do melhor alvo ---
        
        foreach (Collider target in targets)
        {
            if (target.transform == transform) 
                continue; 

            EngagementTracker targetStatus = target.GetComponent<EngagementTracker>();
            if (targetStatus == null)
                continue;

            // REGRA CRÍTICA: FILTRO DE ENGAJAMENTO (Targeted)
            // Se o alvo já está ALVEJADO E não está sendo perseguido por mim, IGNORE.
            if (targetStatus.IsTargeted && targetStatus.CurrentTracker != this.transform)
            {
                continue; // Pula para o próximo alvo na lista.
            }
            
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target.transform;
                bestTargetStatus = targetStatus;
            }
        }
        
        // --- 2. Gerenciamento do Estado (Tracking) ---
        
        // Se o alvo mudou (incluindo se bestTarget se tornou null)
        if (currentTarget != bestTarget)
        {
            // Se tínhamos um alvo, desengajamos (liberamos o alvo anterior)
            if (currentTarget != null && targetEngagement != null)
            {
                targetEngagement.StopTracking(this.transform);
            }
            
            // Atualiza o alvo atual
            currentTarget = bestTarget;
            targetEngagement = bestTargetStatus;

            // Se o novo alvo não é nulo, começamos a rastreá-lo (marcamos o novo alvo)
            if (currentTarget != null && targetEngagement != null)
            {
                targetEngagement.StartTracking(this.transform);
            }
        }
    }
    
    void AttackTarget()
    {
        if (currentTarget == null) return;
        
        Vector3 lookAtPos = currentTarget.position;
        lookAtPos.y = transform.position.y;
        transform.LookAt(lookAtPos);
        
        // ** Aqui entra a lógica de dano/animação **
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}