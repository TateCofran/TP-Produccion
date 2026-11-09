# 07 — Mejoras permanentes: reemplazos y rebalanceo

> **OBSOLETO (07/11/2026).** Reemplazado por `13_meta_progresion_y_loadout.md`. Las mejoras permanentes se eliminan y la progresión pasa a ser XP por modo. No implementar.

GDD: 2.6.1 Permanent upgrades. Depende de los MD 01, 02 y 03.

## Objetivo

- Las mejoras "Lvl Cap" (`tile_level_cap`) y "Tile Dupe System" (desbloqueo del leveling de tiles) **dejan de tener sentido**: los tiles no tienen tope y los duplicados son base. Hay que reemplazarlas en el árbol Lab (Normal Mode).
- Las mejoras "More Essences" pasan a ser **por kill**.
- Los costos tienen que rebalancearse porque el ingreso de esencias por partida cambia mucho.

## Estado actual

- `Assets/Scripts/Upgrades/UpgradeSO.cs`: `upgradeId`, `displayName`, `description`, `category` (`Lab` = azul / tiles, `Workshop` = roja / torretas), `maxLevel` y `costBluePerLevel` / `costRedPerLevel`.
- Los niveles se leen con `UpgradeLevels.Get(id)` o `UpgradeSystemBootstrap.Service.GetCurrentLevel(so)`.
- `TurretPermanentUpgrades.cs`: `turret_global_damage`, `turret_global_range` y `turret_global_fire_rate`.
- `WorldSwitchPoints`: `upgrade_moreEssencePerSwitchTile` / `upgrade_moreEssencePerSwitchTurret`.
- Las UpgradeSO de "Lvl Cap" y "Tile Dupe System" estaban referenciadas desde `TileLevelCapProviderSO` / `TileLevelingPermissionProvider`, que se borran en los MD 02 y 03. Buscar los assets en `Assets/` por `upgradeId`.

## Cambios

1. **Reemplazar los 2 assets obsoletos** reusando los mismos `.asset`, para no romper la lista del menú Laboratory. Cambiar `upgradeId`, `displayName` y `description`:
   - "Lvl Cap" → **Swift Builder**: `upgrade_tileBarSpeed`, `maxLevel` 3. La barra de tiles se llena +10% más rápido por nivel. Leerlo en `ShiftingWorldMechanic` junto con el modificador N01.
   - "Tile Dupe System" → **Treasure Seeker**: `upgrade_rewardZoneBonus`, `maxLevel` 3. +20% de recompensa de las zonas por nivel. Leerlo en `RewardZoneSystem.Grant` junto con N12.
   - **Pendiente de decisión:** si prefiero otras mejoras de la lista del GDD (opción extra de tile, radio de revelado), te aviso antes de que lo implementes.
2. **More Essences**:
   - Actualizar `displayName` / `description` de las 2 UpgradeSO de esencias para que digan "per kill".
   - Los IDs no cambian, porque el MD 01 los sigue leyendo.
3. **Rebalanceo de costos**:
   - Correr una partida de prueba hasta la oleada 15 y otra hasta la 30 (o simular con `WaveRecipeList`: sumar `essenceReward` × cantidad de enemigos por oleada).
   - Pasarme una tabla con las esencias azules + rojas esperadas por partida al morir en la 15, la 30 y la 45, y una propuesta de costos por nivel para cada UpgradeSO.
   - **No cambies los costos hasta que yo apruebe la tabla.**
4. **Mejora General: vida inicial del core** (`upgrade_coreHealth`, categoría nueva `General` en `UpgradeCategory`):
   - +2 de vida máxima por nivel, `maxLevel` 5.
   - `Core.cs` lo lee al iniciar.
   - Cuesta azul **y** rojo. `UpgradeService.CanPurchase` hoy rechaza mezclas por categoría: permitir ambas monedas solo para `General`.
   - Si el menú no tiene dónde mostrar una tercera categoría, dejame un checklist del Editor.

## Criterios de aceptación

- En el menú Laboratory ya no aparecen "Lvl Cap" ni "Tile Dupe System". En su lugar están Swift Builder y Treasure Seeker, y funcionan.
- Las descripciones de More Essences dicen "per kill".
- Me entregaste la tabla de ingresos esperados y costos propuestos.
- La mejora de vida del core sube la vida inicial en la partida siguiente.
