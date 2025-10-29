using UnityEngine;

public class Core : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

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

        if (currentHealth <= 0)
        {
            Debug.Log("Core destroyed!");
            // Notificamos al GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}
