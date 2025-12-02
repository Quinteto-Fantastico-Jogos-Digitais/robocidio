// DEBUG.cs
using System.Collections.Generic;
using UnityEngine;

public class DEBUG : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Parent que contém os zumbis. Se vazio, buscará por tag 'Zombie'.")]
    public Transform parentContainer;

    // Cached component lists
    List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    List<Animator> animators = new List<Animator>();
    List<Collider> colliders = new List<Collider>();
    List<Rigidbody> rigidbodies = new List<Rigidbody>();
    List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    List<AudioSource> audioSources = new List<AudioSource>();

    // Original states to restore
    Dictionary<Renderer, bool> originalRendererState = new Dictionary<Renderer, bool>();
    Dictionary<Animator, bool> originalAnimatorState = new Dictionary<Animator, bool>();
    Dictionary<Collider, bool> originalColliderState = new Dictionary<Collider, bool>();
    Dictionary<Rigidbody, (bool isKinematic, bool detectCollisions)> originalRigidbodyState = new Dictionary<Rigidbody, (bool, bool)>();
    Dictionary<ParticleSystem, bool> originalParticleActive = new Dictionary<ParticleSystem, bool>();
    Dictionary<AudioSource, bool> originalAudioMute = new Dictionary<AudioSource, bool>();

    // Shadows backup
    private UnityEngine.ShadowQuality prevShadowQuality;
    private float prevShadowDistance;
    private bool shadowsBackedUp = false;


    void Start()
    {
        RefreshTargets();
    }

    void Update()
    {
        // Key mapping
        if (Input.GetKeyDown(KeyCode.F1)) ToggleRenderers(false);
        if (Input.GetKeyDown(KeyCode.F2)) ToggleRenderers(true);

        if (Input.GetKeyDown(KeyCode.F3)) ToggleAnimators(false);
        if (Input.GetKeyDown(KeyCode.F4)) ToggleAnimators(true);

        if (Input.GetKeyDown(KeyCode.F5)) TogglePhysics(false);
        if (Input.GetKeyDown(KeyCode.F6)) TogglePhysics(true);

        if (Input.GetKeyDown(KeyCode.F7)) ToggleShadows(false);
        if (Input.GetKeyDown(KeyCode.F8)) ToggleShadows(true);

        if (Input.GetKeyDown(KeyCode.T)) RefreshTargets();
    }

    // Re-popula as listas de componentes (call se spawn/despawn ocorrer)
    public void RefreshTargets()
    {
        skinnedRenderers.Clear();
        meshRenderers.Clear();
        animators.Clear();
        colliders.Clear();
        rigidbodies.Clear();
        particleSystems.Clear();
        audioSources.Clear();

        originalRendererState.Clear();
        originalAnimatorState.Clear();
        originalColliderState.Clear();
        originalRigidbodyState.Clear();
        originalParticleActive.Clear();
        originalAudioMute.Clear();

        Transform[] targets;
        if (parentContainer != null)
        {
            var children = parentContainer.GetComponentsInChildren<Transform>(true);
            targets = children;
        }
        else
        {
            var gos = GameObject.FindGameObjectsWithTag("Zombie");
            targets = new Transform[gos.Length];
            for (int i = 0; i < gos.Length; i++) targets[i] = gos[i].transform;
        }

        foreach (var t in targets)
        {
            if (t == null) continue;

            var smr = t.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                skinnedRenderers.Add(smr);
                if (!originalRendererState.ContainsKey(smr)) originalRendererState[smr] = smr.enabled;
            }

            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                meshRenderers.Add(mr);
                if (!originalRendererState.ContainsKey(mr)) originalRendererState[mr] = mr.enabled;
            }

            var anim = t.GetComponent<Animator>();
            if (anim != null)
            {
                animators.Add(anim);
                if (!originalAnimatorState.ContainsKey(anim)) originalAnimatorState[anim] = anim.enabled;
            }

            var col = t.GetComponent<Collider>();
            if (col != null)
            {
                colliders.Add(col);
                if (!originalColliderState.ContainsKey(col)) originalColliderState[col] = col.enabled;
            }

            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rigidbodies.Add(rb);
                if (!originalRigidbodyState.ContainsKey(rb)) originalRigidbodyState[rb] = (rb.isKinematic, rb.detectCollisions);
            }

            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                particleSystems.Add(ps);
                if (!originalParticleActive.ContainsKey(ps)) originalParticleActive[ps] = ps.isPlaying;
            }

            var aud = t.GetComponent<AudioSource>();
            if (aud != null)
            {
                audioSources.Add(aud);
                if (!originalAudioMute.ContainsKey(aud)) originalAudioMute[aud] = aud.mute;
            }
        }

        Debug.Log($"[DEBUG] Targets refreshed: SKR={skinnedRenderers.Count}, MR={meshRenderers.Count}, A={animators.Count}, COL={colliders.Count}, RB={rigidbodies.Count}, PS={particleSystems.Count}, AUD={audioSources.Count}");
    }

    // Renderers
    public void ToggleRenderers(bool enable)
    {
        foreach (var r in skinnedRenderers)
            if (r != null) r.enabled = enable;
        foreach (var r in meshRenderers)
            if (r != null) r.enabled = enable;

        Debug.Log($"[DEBUG] Renderers set to {enable}");
    }

    // Animators
    public void ToggleAnimators(bool enable)
    {
        foreach (var a in animators)
            if (a != null) a.enabled = enable;

        Debug.Log($"[DEBUG] Animators set to {enable}");
    }

    // Physics (colliders + rigidbody kinematic)
    public void TogglePhysics(bool enable)
    {
        // colliders: enable = true -> collider.enabled = true
        foreach (var c in colliders)
            if (c != null) c.enabled = enable;

        foreach (var rb in rigidbodies)
        {
            if (rb == null) continue;
            if (enable)
            {
                // restaura valores originais se existirem
                if (originalRigidbodyState.TryGetValue(rb, out var st))
                {
                    rb.isKinematic = st.isKinematic;
                    rb.detectCollisions = st.detectCollisions;
                }
                else
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                }
            }
            else
            {
                // desativa física ativa
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }

        Debug.Log($"[DEBUG] Physics set to {enable}");
    }

    // ParticleSystems + AudioSources
    public void ToggleVFXAudio(bool enable)
    {
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            if (enable)
            {
                // tentar restaurar (simple)
                if (originalParticleActive.TryGetValue(ps, out var wasPlaying) && wasPlaying)
                {
                    ps.Play(true);
                }
                else
                {
                    // mantém parado
                }
            }
            else
            {
                ps.Pause(true);
            }
        }

        foreach (var a in audioSources)
        {
            if (a == null) continue;
            if (enable)
            {
                if (originalAudioMute.TryGetValue(a, out var wasMuted)) a.mute = wasMuted;
                else a.mute = false;
            }
            else
            {
                a.mute = true;
            }
        }

        Debug.Log($"[DEBUG] VFX/Audio set to {enable}");
    }

    // Shadows global (QualitySettings)
    public void ToggleShadows(bool enable)
    {
        if (!shadowsBackedUp)
        {
            prevShadowQuality = QualitySettings.shadows;
            prevShadowDistance = QualitySettings.shadowDistance;
            shadowsBackedUp = true;
        }

        if (enable)
        {
            QualitySettings.shadows = prevShadowQuality;
            QualitySettings.shadowDistance = prevShadowDistance;
            Debug.Log("[DEBUG] Shadows restored to previous quality.");
        }
        else
        {
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            Debug.Log("[DEBUG] Shadows disabled via QualitySettings.");
        }
    }

    // opcional: restaura tudo para estados originais
    public void RestoreAllOriginals()
    {
        foreach (var kv in originalRendererState)
            if (kv.Key != null) kv.Key.enabled = kv.Value;

        foreach (var kv in originalAnimatorState)
            if (kv.Key != null) kv.Key.enabled = kv.Value;

        foreach (var kv in originalColliderState)
            if (kv.Key != null) kv.Key.enabled = kv.Value;

        foreach (var kv in originalRigidbodyState)
        {
            if (kv.Key == null) continue;
            kv.Key.isKinematic = kv.Value.isKinematic;
            kv.Key.detectCollisions = kv.Value.detectCollisions;
        }

        foreach (var kv in originalParticleActive)
            if (kv.Key != null && kv.Value) kv.Key.Play(true);

        foreach (var kv in originalAudioMute)
            if (kv.Key != null) kv.Key.mute = kv.Value;

        if (shadowsBackedUp)
        {
            QualitySettings.shadows = prevShadowQuality;
            QualitySettings.shadowDistance = prevShadowDistance;
        }

        Debug.Log("[DEBUG] Restored all cached original states.");
    }
}
