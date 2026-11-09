# 01 — Esencias por kill

GDD: 2.2.1 Essences.

## Objetivo

Las esencias se ganan **solo al matar enemigos**, según el modo activo en el momento del kill:

- `WorldState.Current == World.Normal` → esencia azul (Normal Essence).
- `WorldState.Current == World.Other` → esencia roja (Other World Essence).

Se **elimina** la ganancia de esencias por cambiar de mundo.

## Estado actual

- `Assets/Scripts/SwitchMechanic/WorldSwitchPoints.cs`: en `Update()` detecta el cambio de mundo y suma `essencesOnNormal` / `essencesOnOther`, con los multiplicadores de las mejoras `upgrade_moreEssencePerSwitchTile` y `upgrade_moreEssencePerSwitchTurret`. Expone `TotalBlue`, `TotalRed`, `AddBlueEssence` y `AddRedEssence`.
- `Assets/Scripts/GameManager.cs` (alrededor de las líneas 249 y 283): busca `WorldSwitchPoints` con `FindFirstObjectByType` para pasar los totales a `EssenceBank.Add` al terminar la partida, y para resetearlos al reiniciar.
- `Assets/Scripts/Enemies/Enemy.cs`: `public static event Action<Enemy> OnAnyEnemyKilled` se dispara en `NotifyDeath()`. **Ojo:** también se dispara cuando el enemigo llega al core (`HitCore` llama a `NotifyDeath`). Para distinguirlo está `e.ReachedCore` (ver cómo lo usa `SpawnManager.cs` en la línea 70).
- `Ability_SecondLifeBerserk` (`Enemies/Abilities/SecondLife.cs`) evita la primera muerte con `OnPreDeath`, así que `NotifyDeath` se llama una sola vez, en la muerte final. No hace falta un caso especial.
- `Ability_SpawnOnDeath` y `SpawnMinionsPeriodic` crean enemigos con `EnemyPool.Instance.GetEnemy(type)` + `enemy.Init(route)`.
- `Assets/Scripts/UI/EssencePopupUI.cs`: `SetWorld(bool isNormal)` + `HandleEssence(delta, newTotal)`.

## Cambios

1. **`EnemyData.cs`**: agregar `public int essenceReward = 1;` debajo de `damageToCore`.
2. **`Enemy.cs`**:
   - Agregar `public bool IsSummoned { get; private set; }` y `public void MarkAsSummoned() => IsSummoned = true;`.
   - En `Init(...)`, resetear `IsSummoned = false` (los enemigos salen de un pool).
3. **`SpawnOnDeath.cs` y `SpawnMinionsPeriodic.cs`**: después de `enemy.Init(route)`, llamar a `enemy.MarkAsSummoned()`.
4. **`WorldSwitchPoints.cs`**: refactor en el mismo archivo, sin renombrar la clase, porque está en la escena y `GameManager` la busca por tipo.
   - Borrar la lógica de `Update()` que suma esencias al cambiar de mundo. `_lastWorld` sigue sirviendo para llamar a `essenceUI.SetWorld(...)` cuando cambia el modo. Mejor todavía: suscribirse a `WorldState.OnWorldChanged` y borrar el `Update`.
   - Suscribirse a `Enemy.OnAnyEnemyKilled` en `OnEnable` y desuscribirse en `OnDisable`.
   - En el handler: ignorar si `e.ReachedCore`, si `e.IsSummoned` o si `e.Data == null`. Si no, calcular `amount = Mathf.RoundToInt(e.Data.essenceReward * multiplier)`, con un mínimo de 1, y sumarlo al total del modo activo.
   - El multiplicador combina:
     - las mejoras permanentes existentes (`labBlueUpgradeId` / `workshopRedUpgradeId`; los IDs se mantienen y solo cambia su significado a "por kill");
     - `GameModifiersManager.Instance.blueEssenceMultiplier` / `redEssenceMultiplier`. Agregar esos dos campos (`float`, default `1f`) en `GameModifiersManager.cs`. Los usa el MD 04.
   - Renombrar los campos `essencesOnNormal` / `essencesOnOther` está fuera de alcance. Borrarlos, ya no se usan.
   - Disparar `OnBlueEssenceGained` / `OnRedEssenceGained` en cada kill.
   - **Bug a corregir:** `GameManager` resetea con `AddBlueEssence(-TotalBlue)`, pero `AddBlueEssence` sale si `amount <= 0`, así que el reset no hace nada. Agregar `public void ResetTotals()` en `WorldSwitchPoints` y usarlo desde `GameManager`.
5. **Popup**: con muchos kills, el popup se reinicia cada frame. Acumular lo ganado durante una ventana (por ejemplo 0.5 s, configurable con `[SerializeField]`) y mostrar un solo `HandleEssence` con el total de la ventana. Usar tiempo unscaled, como el resto de `EssencePopupUI`.
6. **Valores en los assets**: setear `essenceReward` en los 15 `EnemyData` de `Assets/ScriptableObject/Enemies/Zone1..3` según su `EnemyType`:

| EnemyType | essenceReward |
| --- | --- |
| Minion, Minion2, Minion3 | 1 |
| Sprint, Sprint2, Sprint3 | 1 |
| Big, Big2, Big3 | 3 |
| MiniBoss, MiniBoss2, MiniBoss3 | 15 |
| Boss, Boss2, Boss3 | 50 |

   Si los `.asset` no se pueden editar de forma segura desde código, agregar un menú de Editor (`Tools/Shadow Realms/Apply Essence Rewards`) en `Assets/Scripts/Editor/` que los asigne por `type`.

## Criterios de aceptación

- Cambiar de mundo con `J` ya no da esencias.
- Matar un Minion en modo Normal suma 1 azul; en modo Other suma 1 roja.
- Un enemigo que llega al core no da esencias.
- Los minions que crea el Boss Goblin y los que deja el Progenitor al morir dan 0.
- El Black Knight da esencias una sola vez, en la muerte final.
- Al terminar la partida, `ResultScene` muestra los totales y `EssenceBank` los acumula, igual que antes.
- Reiniciar la partida desde el `GameManager` resetea los totales a 0.
