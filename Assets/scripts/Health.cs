using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [Tooltip("Vida máxima que o objeto pode ter.")]
    public float maxHealth = 100f;
    
    private float currentHealth; 

    public VariavelGlobal variaveisGlobais;
    public ControladorHorda Controla;

    public float CurrentHealth 
    {
        get { return currentHealth; }
    }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{gameObject.name} recebeu {damage} de dano. Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} foi curado em {healAmount}. Vida atual: {currentHealth}");
    }

    public void Upgrade()
    {
        maxHealth += 50;
        currentHealth = maxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} aumentou a vida em 50. Vida atual: {currentHealth}");
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} morreu!");
        Controla.CallGameOver();
        variaveisGlobais.CallGameOver();
    }
}