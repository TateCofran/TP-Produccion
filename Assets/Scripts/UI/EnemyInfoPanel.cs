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

    [SerializeField] private TextMeshProUGUI abilityText;

    public void Display(EnemyData data)
    {
        nameText.text = data.enemyName;

        healthText.text = $"<b><color=#D19A21>Health:</color></b> <color=white>{data.maxHealth}</color>";
        defenseText.text = $"<b><color=#D19A21>Defense:</color></b> <color=white>{data.defense}</color>";
        speedText.text = $"<b><color=#D19A21>Speed:</color></b> <color=white>{data.moveSpeed}</color>";
        damageText.text = $"<b><color=#D19A21>Damage to core:</color></b> <color=white>{data.damageToCore}</color>";

        abilityText.text = $"Ability: {data.abilityDescription}";
    }
}
