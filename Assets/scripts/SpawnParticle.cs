using UnityEngine;

public class SpawnParticle : MonoBehaviour
{
    public GameObject bloodParticlePrefab; // prefab com ParticleSystem

    // chama quando ocorrer o corte
    public void SpawnAt(Vector3 position, Vector3 normal)
    {
        var go = Instantiate(bloodParticlePrefab, position, Quaternion.LookRotation(normal));
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        Destroy(go, 5f); // destrói depois do efeito (ou use pooling)
    }
}