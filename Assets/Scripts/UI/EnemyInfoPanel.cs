using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;
    //[SerializeField] private Image iconImage; 
    [SerializeField] private TextMeshProUGUI abilityText;

    public void Display(EnemyData data)
    {
        nameText.text = data.enemyName;
        healthText.text = $"Health: {data.maxHealth}";
        defenseText.text = $"Defense: {data.defense}";
        speedText.text = $"Speed: {data.moveSpeed}";
        damageText.text = $"Damage to core: {data.damageToCore}";

        abilityText.text = $"Ability: {data.abilityDescription}";
    }

    
}
