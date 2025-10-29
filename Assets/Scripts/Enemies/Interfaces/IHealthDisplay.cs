using UnityEngine;

public interface IHealthDisplay
{
    void Initialize(Transform parent, float maxHealth);
    void UpdateHealthBar(float currentHealth, float maxHealth);
    void DestroyBar();
}
