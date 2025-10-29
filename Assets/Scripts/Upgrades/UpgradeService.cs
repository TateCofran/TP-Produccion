using System;
using UnityEngine;

public interface IUpgradeService
{
    event Action<UpgradeSO, int> OnUpgradePurchased; // nuevo nivel
    int GetCurrentLevel(UpgradeSO upgrade);
    bool CanPurchase(UpgradeSO upgrade, out int costBlue, out int costRed, out string reason);
    bool TryPurchase(UpgradeSO upgrade);
}

public class UpgradeService : IUpgradeService
{
    private readonly IUpgradeStateStore _store;

    public event Action<UpgradeSO, int> OnUpgradePurchased;

    public UpgradeService(IUpgradeStateStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int GetCurrentLevel(UpgradeSO upgrade)
    {
        if (upgrade == null) return 0;
        return _store.GetLevel(upgrade.Id);
    }

    public bool CanPurchase(UpgradeSO upgrade, out int costBlue, out int costRed, out string reason)
    {
        costBlue = costRed = 0;
        reason = null;
        if (upgrade == null) { reason = "Upgrade null"; return false; }

        int current = _store.GetLevel(upgrade.Id);
        if (current >= upgrade.MaxLevel) { reason = "Max level reached"; return false; }

        int next = current + 1;
        var (blue, red) = upgrade.GetCostForLevel(next);
        costBlue = blue; costRed = red;

        // Restricción: Lab usa azules; Workshop usa rojas (podés permitir mixto si querés)
        if (upgrade.Category == UpgradeCategory.Lab && red > 0)
            reason = "Lab upgrades should not cost red essences";
        if (upgrade.Category == UpgradeCategory.Workshop && blue > 0)
            reason = "Workshop upgrades should not cost blue essences";

        bool enough =
            EssenceBank.TotalBlue >= blue &&
            EssenceBank.TotalRed >= red;

        if (!enough) reason = "Not enough essences";
        return enough && string.IsNullOrEmpty(reason);
    }

    public bool TryPurchase(UpgradeSO upgrade)
    {
        if (!CanPurchase(upgrade, out int blue, out int red, out _)) return false;

        // Gastar
        if (!EssenceBank.TrySpend(blue, red)) return false;

        // Subir nivel y persistir
        int current = _store.GetLevel(upgrade.Id);
        int next = Mathf.Clamp(current + 1, 0, upgrade.MaxLevel);
        _store.SetLevel(upgrade.Id, next);

        OnUpgradePurchased?.Invoke(upgrade, next);
        return true;
    }
}
