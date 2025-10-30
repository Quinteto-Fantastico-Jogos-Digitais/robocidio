using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] private Transform currentTarget;
    private EngagementTracker targetEngagement;
    private EngagementTracker myEngagementTracker;
    
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

        agent.stoppingDistance = attackRange;
    }

    void Update()
    {
        FindNewTarget();

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= attackRange + 0.1f)
            {
                AttackTarget(); 
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
        // 1. REGRA DE RETALIAÇÃO (PRIORIDADE MÁXIMA)
        if (myEngagementTracker.IsTargeted && myEngagementTracker.CurrentTracker != null)
        {
            Transform retaliator = myEngagementTracker.CurrentTracker;
            EngagementTracker retaliatorStatus = retaliator.GetComponent<EngagementTracker>();
            
            // CRUCIAL: Trava o rastreador atual
            myEngagementTracker.StartTracking(retaliator);
            
            if (currentTarget != retaliator)
            {
                // Libera o alvo anterior (se eu estava perseguindo outro alguém antes de ser atacado)
                if (currentTarget != null && targetEngagement != null)
                {
                    targetEngagement.StopTracking(this.transform);
                }

                // Configura a retaliação
                currentTarget = retaliator;
                targetEngagement = retaliatorStatus;
            }
            // O retorno garante que o cubo NUNCA mude de alvo enquanto estiver sendo atacado
            return; 
        }
        
        // 2. BUSCA NORMAL (Se não está em retaliação)
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, targetLayerMask);
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        EngagementTracker bestTargetStatus = null;
        
        // A. Manter o Alvo Atual (Sticky Target)
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
        
        // B. Procurar Novos Alvos (ou um alvo mais próximo)
        foreach (Collider target in targets)
        {
            if (target.transform == transform || target.transform == currentTarget) 
                continue; 

            EngagementTracker targetStatus = target.GetComponent<EngagementTracker>();
            if (targetStatus == null)
                continue;

            // REGRA DE EXCLUSÃO
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
        
        // 3. ATUALIZAÇÃO E MARCAÇÃO DE ESTADO (Com Correção de Roubo)
        if (currentTarget != bestTarget)
        {
            // Libera o alvo anterior SOMENTE se EU sou o rastreador.
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
        }
    }
    
    private bool IsTargetInSphere(Transform targetTransform, Collider[] targets)
    {
        foreach (Collider collider in targets)
        {
            if (collider.transform == targetTransform)
            {
                return true;
            }
        }
        return false;
    }
    
    void AttackTarget()
    {
        if (currentTarget == null) return;
        
        Vector3 lookAtPos = currentTarget.position;
        lookAtPos.y = transform.position.y;
        transform.LookAt(lookAtPos);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}