using Unity.Cinemachine;
using UnityEngine;

public class Core : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Screen Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Start()
    {
        currentHealth = maxHealth;
        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();

        }
        else
        {
            Debug.LogWarning("Core: no hay CinemachineImpulseSource asignado en el inspector.");
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Core destroyed!");
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}
