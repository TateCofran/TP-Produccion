using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeRepository", menuName = "Upgrades/Repository")]
public class UpgradeRepository : ScriptableObject
{
    [SerializeField] private List<UpgradeSO> upgrades = new();

    public IEnumerable<UpgradeSO> GetAll() => upgrades.Where(u => u != null);

    public IEnumerable<UpgradeSO> GetByCategory(UpgradeCategory category) =>
        upgrades.Where(u => u != null && u.Category == category);

    public UpgradeSO GetById(string id) => upgrades.FirstOrDefault(u => u != null && u.Id == id);
}
