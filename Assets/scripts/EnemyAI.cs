using UnityEngine;
using UnityEngine.AI; // <--- Certifique-se que está assim!

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform currentTarget;

    [Header("Detecção e Alvos")]
    public float detectionRadius = 15f; // <--- ADICIONE O PONTO E VÍRGULA
    public LayerMask targetLayerMask; // <--- ADICIONE O PONTO E VÍRGULA
    
    [Header("Configurações de Combate")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
    }

    void FindNewTarget()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, targetLayerMask);
        
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (Collider target in targets)
        {
            if (target.transform == transform) 
                continue; 
            
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = target.transform;
            }
        }

        currentTarget = bestTarget;
    }
    
    void AttackTarget()
    {
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