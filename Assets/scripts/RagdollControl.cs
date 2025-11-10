using UnityEngine;

public class RagdollControl : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;
    private Animator animator; 

    void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>(); 

        SetRagdollState(false);
    }

    public void SetRagdollState(bool isRagdoll)
    {
        if (animator != null)
        {
            animator.enabled = !isRagdoll;
        }

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !isRagdoll; 
        }

        foreach (Collider col in colliders)
        {
            col.enabled = isRagdoll; 
        }

        if (isRagdoll)
        {
            Rigidbody hipsRb = GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Hips)?.GetComponent<Rigidbody>();
            if (hipsRb != null)
            {
                hipsRb.AddForce(transform.forward * -500f, ForceMode.Impulse);
                hipsRb.AddTorque(Random.insideUnitSphere * 100f, ForceMode.Impulse);
            }
        }
    }
}