using UnityEngine;

public class EngagementTracker : MonoBehaviour
{
    // Indica se algum inimigo o escolheu como alvo principal.
    public bool IsTargeted { get; private set; } = false;

    // A Transform do inimigo que está atualmente engajado ou perseguindo este alvo.
    [HideInInspector] public Transform CurrentTracker = null;

    // Chamado pelo script EnemyAI quando ele é o alvo mais próximo e começa a persegui-lo.
    public void StartTracking(Transform tracker)
    {
        IsTargeted = true;
        CurrentTracker = tracker;
    }

    // Chamado pelo script EnemyAI quando ele perde o alvo, muda de alvo ou morre.
    public void StopTracking(Transform potentialTracker)
    {
        // Importante: Somente o rastreador atual pode liberar o alvo.
        if (CurrentTracker == potentialTracker)
        {
            IsTargeted = false;
            CurrentTracker = null;
        }
    }
}