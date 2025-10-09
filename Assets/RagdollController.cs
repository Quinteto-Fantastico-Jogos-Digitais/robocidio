using UnityEngine;

public class RagdollController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody[] rigidbodies;
    private Collider[] boneColliders;
    private Collider mainCollider; // O Collider principal, ex: CapsuleCollider

    void Awake()
    {
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>(); 
        
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        boneColliders = GetComponentsInChildren<Collider>();

        SetRagdollMode(false);
    }

    public void SetRagdollMode(bool isRagdoll)
    {
        // 1. Controla o Animator
        animator.enabled = !isRagdoll;

        // 2. Controla o Collider principal
        if (mainCollider != null)
        {
            mainCollider.enabled = !isRagdoll;
        }

        // 3. Controla a física nos ossos (isKinematic)
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !isRagdoll;
        }

        // 4. Controla os Colliders dos ossos
        foreach (Collider col in boneColliders)
        {
            if (col != mainCollider)
            {
                col.enabled = isRagdoll;
            }
        }
    }
}