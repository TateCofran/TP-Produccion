# 04 — Modificadores cada 3 oleadas (15 por modo)

GDD: 2.6.3 Modifiers.

## Objetivo

- Al terminar las oleadas 3, 6, 9 … 42, el juego se pausa y muestra **3 modificadores del modo activo** (`WorldState.Current`). El jugador elige uno. No se ofrece después de la oleada 45.
- 15 modificadores para el modo Normal (N01–N15) y 15 para Other World (O01–O15), todos distintos.
- **Se combinan todos entre sí.** Los bonus al mismo stat se suman y los de stats distintos se aplican por separado.
- Cada modificador se puede elegir **una vez por partida**. Los ya elegidos no se vuelven a ofrecer.

## Estado actual

- `Assets/Scripts/Modifiers/GameModifiersManager.cs`:
  - Singleton con `turretRangeMultiplier`, `turretFireRateMultiplier`, `turretDamageMultiplier`, `enemyCountMultiplier`, `enemySpeedMultiplier` y `enemyDamageTakenMultiplier`.
  - `allModifiers` nunca se llena.
  - Hoy solo se usan `turret*Multiplier` (en `TurretStats.RecalculateStats`) y `enemyDamageTakenMultiplier` (en `EnemyHealth.TakeDamage`). `enemyCountMultiplier` y `enemySpeedMultiplier` **no los lee nadie**.
- `Assets/Scripts/Modifiers/IGameModifier.cs`: `Name`, `Description`, `Category`, `Apply(manager)` y `GetStackDescription(stacks)`. Tiene el enum `ModifierCategory { NormalWorld, OtherWorld, ShiftWorld, LegendaryNormal, LegendaryOther }`.
- `Assets/Scripts/Modifiers/ModifierPanelSelection.cs`:
  - `OpenPanel(callback)` instancia cards, pero **no les asigna click**, así que `OnModifierSelected` nunca se llama.
  - Sortea de `allModifiers` sin filtrar por modo.
  - Tiene `ShowLegendaryModifier`, que no se usa.
- Las carpetas `Modifiers/Normal World`, `Modifiers/Other World`, `Modifiers/Shift Worlds` y `Modifiers/Generics` están vacías.
- Nadie llama a `OpenPanel`, y el panel no está en ninguna escena.
- `WaveManager`: evento `OnWaveEnded`, `GetCurrentWave()`, `MaxWaves` e `IsLastWave()`.

## Diseño

Un solo tipo de asset alcanza para los 30 modificadores: cada uno es una lista de entradas (stat, valor), y los sistemas del juego leen el total de cada stat.

1. **`Modifiers/ModifierStat.cs`**: enum con todos los stats de la tabla de abajo.
2. **`Modifiers/ModifierSO.cs`**: `ScriptableObject` que implementa `IGameModifier`, con `[CreateAssetMenu(menuName = "Modifiers/Modifier")]`.
   - Campos: `id` (N01…), `displayName`, `[TextArea] description`, `icon` (Sprite), `category` (`NormalWorld` u `OtherWorld`) y `List<ModifierEntry> entries` (`ModifierStat stat`, `float value`).
   - `Apply(manager)` suma cada entrada con `manager.AddStat(stat, value)`.
3. **`GameModifiersManager`**:
   - `Dictionary<ModifierStat, float> stats`, `AddStat`, `GetStat(stat)` (default 0) y `GetMultiplier(stat) => 1f + GetStat(stat)`.
   - Los campos públicos actuales pasan a ser propiedades calculadas: por ejemplo, `turretDamageMultiplier => GetMultiplier(ModifierStat.TurretDamage)`. Así `TurretStats` y `EnemyHealth` siguen compilando.
   - `[SerializeField] List<ModifierSO> normalPool` y `otherPool` se asignan en el Inspector. Borrar `modifiersNormalWorld`, `modifiersOtherWorld`, `modifiersShiftWorld` y `allModifiers`.
   - `GetAvailable(ModifierCategory category)` devuelve el pool del modo menos los ya aplicados.
   - Evento `OnModifierApplied(ModifierSO)`.
   - Después de aplicar un modificador que toca stats de torretas, llamar a `RecalculateStats()` en todas las `TurretStats` de la escena (hoy los stats se calculan solo en `SetLevel`).
   - Agregar `blueEssenceMultiplier` / `redEssenceMultiplier` como propiedades calculadas, si no quedaron del MD 01.
   - Resetear todo al iniciar la partida.
4. **`Modifiers/ModifierWaveTrigger.cs`** (MonoBehaviour en GameScene):
   - Se suscribe a `WaveManager.Instance.OnWaveEnded`.
   - Si `wave % 3 == 0 && !IsLastWave()`, llama a `GameTime.RequestPause()` y abre el panel con 3 opciones de `GetAvailable(categoría del modo activo)`.
   - Al elegir, `ReleasePause()`.
   - Intervalo configurable (`everyNWaves = 3`).
   - Verificar que `WaveManager.NextWaveCountdownRoutine` no siga corriendo durante la pausa. Si usa tiempo unscaled, frenarlo mientras el panel está abierto.
5. **`ModifierPanelSelection`**:
   - Crear `Modifiers/ModifierCardUI.cs` con `Bind(ModifierSO mod, Action onClick)`, que llena nombre, descripción e ícono y engancha el `Button`.
   - `OpenPanel(List<ModifierSO> options, Action onChosen)`: instancia la card según la categoría y llama a `Bind`.
   - Click: `ApplyModifier`, cerrar el panel, invocar el callback.
   - Borrar `ShowLegendaryModifier` y `GetRandomModifiers`.
   - Agregar `OpenExtraPick()`. Lo usa el MD 06 (zona de recompensa).
6. **HUD**: `Modifiers/ActiveModifiersHUD.cs` muestra el ícono de cada modificador aplicado, con tooltip de nombre y descripción. Lo escucha de `OnModifierApplied`.
7. **Assets**: crear un menú de Editor `Tools/Shadow Realms/Create Modifier Assets` en `Assets/Scripts/Editor/` que genere los 30 assets en `Assets/ScriptableObject/Modifiers/Normal/` y `.../Other/` con los valores de la tabla. Si el asset ya existe, no lo pisa.
8. **Editor (checklist para mí)**: armar en GameScene el panel (Canvas, fondo oscuro, contenedor horizontal), los prefabs de card y `GameModifiersManager` + `ModifierWaveTrigger` con sus pools asignados.

## Modificadores y dónde engancharlos

Los valores son iniciales. `+0.2` en un stat multiplicador significa +20%.

### Modo Normal (tiles)

| ID | Nombre | Entradas | Dónde se aplica |
| --- | --- | --- | --- |
| N01 | Fast Builder | TileBarSpeed +0.2 | `ShiftingWorldMechanic.Update`: multiplica `activeDelta` de la barra Normal |
| N02 | Wide Choice | TileOptionsCount +1 | `ShiftingWorldUI.BuildNormalOptions`: hoy tiene `3` fijo y usa el array `tileButtons`. Usar `3 + count`, limitado por `tileButtons.Length` (agregar el 4.º botón en el Editor) |
| N03 | Reroll | TileRerolls +1 | Botón "Reroll" en el panel de tiles: vuelve a llamar a `BuildNormalOptions`. Usos por apertura = stat |
| N04 | Burning Ground | TileDamage +0.25 | `PathEffectManager.GetEffective`: multiplica `outDamagePerHit` |
| N05 | Frozen Ground | TileSlow +0.15 | `GetEffective`: multiplica `outSlow` (respetando `slowMax`) |
| N06 | Shock Stones | TileStunSeconds +0.3 | `GetEffective`: suma segundos a `outStun` si es mayor que 0 (respetando `stunMax`) |
| N07 | Toxic Trail | BasicTileDamage +1 | `PathCellEffect`: si la celda no tiene dps, slow ni stun, aplicar ese daño por hit con el cooldown de daño |
| N08 | Lingering Curse | EffectLingerSeconds +1 | `PathCellEffect` / `EnemyEffects`: el slow y el stun duran X s más después de salir de la celda |
| N09 | Twin Tiles | TileExtraDupeChance +0.2 | `TileDupeSystem.AddDupe`: con esa probabilidad, suma un duplicado extra |
| N10 | Heavy Soil | EnemySpeed -0.08 | `Enemy.Update`: multiplicar `speedMultiplier` por `GetMultiplier(EnemySpeed)` (la propiedad `enemySpeedMultiplier`) |
| N11 | Fog Harvest | FogRevealEssence +1 | MD 05: cada fog tile revelado da esa cantidad de esencia azul |
| N12 | Pathfinder | RewardZoneAlwaysVisible +1, RewardZoneBonus +0.5 | MD 06 |
| N13 | Essence Road | BlueEssence +0.25 | MD 01: `WorldSwitchPoints` |
| N14 | Narrow Path | TileOptionsCount -1, TileBarSpeed +0.35 | Igual que N01 y N02. Mínimo 1 opción |
| N15 | Shift Charge | ShiftChargeTileBar +15 | `ShiftingWorldMechanic`: al cambiar a Normal, `normalProgress += stat` |

### Modo Other World (torretas)

| ID | Nombre | Entradas | Dónde se aplica |
| --- | --- | --- | --- |
| O01 | Fast Forge | TurretBarSpeed +0.2 | `ShiftingWorldMechanic.Update`: `activeDelta` de la barra Other |
| O02 | Armory | TurretOptionsCount +1 | `ShiftingWorldUI.BuildOtherOptions`, igual que N02 |
| O03 | Sharpened | TurretDamage +0.15 | Ya lo lee `TurretStats.RecalculateStats` |
| O04 | Overclock | TurretFireRate +0.15 | Ídem |
| O05 | Eagle Eye | TurretRange +0.15 | Ídem |
| O06 | Wildfire | FireSpreadOnDeath +1 | `Turret/Variants/FireTurret.cs`: si un enemigo muere con burn activo, aplicar el burn al enemigo vivo más cercano dentro de un radio |
| O07 | Chain Lightning | TeslaExtraTargets +1 | `Turret/Variants/TeslaTurret.cs`: sumar al número de objetivos o cadenas |
| O08 | Deadeye | SniperNthShotDouble +4 | Disparo de la Sniper: cada N disparos, daño x2. Hoy `TurretType` no tiene `Sniper`: agregarlo y asignarlo en las 5 `TurretDataSO` |
| O09 | Power Grid | EnergyRadius +0.3 | `Turret/Variants/TurretEnergy.cs`: multiplicar el radio del efecto |
| O10 | Twin Forge | TurretExtraDupeChance +0.2 | `TurretDupeSystem.AddDupe` (o `ShiftingWorldUI.HandleDupeSystem`): duplicado extra con esa probabilidad |
| O11 | Executioner | ExecuteBonus +0.5 | Daño de torretas a enemigos con menos del 25% de vida |
| O12 | Boss Hunter | BossBonus +0.3 | Daño de torretas a `EnemyType` MiniBoss* / Boss* |
| O13 | Essence Reaper | RedEssence +0.25 | MD 01 |
| O14 | Blood Price | TurretDamage +0.4, CoreMaxHealth -3 | `Core.cs`: agregar `ModifyMaxHealth(int delta)` (mínimo 1, ajusta la vida actual y actualiza `UIController`) |
| O15 | Horde Bounty | EnemyCount +0.3, BlueEssence +1, RedEssence +1 | `WaveManager.CalculateEnemiesThisWave` y `SpawnManager`: spawnear de verdad un 30% más de cada recipe, no solo contar más |

O11 y O12 tienen que afectar **solo al daño de torretas**, no al de los tiles. Crear `Turret/TurretDamageCalculator.cs` con `static float Compute(float baseDamage, Enemy target)` y usarlo en cada lugar donde una torreta aplica daño (`Projectiles/Projectile.cs`, `Turret/BasicRaycastShooter.cs`, `FireTurret`, `TeslaTurret`). Buscá todos los `TakeDamage(` de torretas.

## Criterios de aceptación

- Al terminar la oleada 3, el juego se pausa y aparecen 3 cards del modo activo. Al elegir una, se aplica y el juego sigue.
- Si estoy en modo Other, solo aparecen modificadores O.
- Un modificador elegido no vuelve a aparecer en la misma partida.
- Elegir O03 y después O14 suma +55% de daño en el `TurretInfoPanel`.
- N10 hace que todos los enemigos caminen más lento.
- O15 aumenta la cantidad real de enemigos y duplica las esencias por kill.
- Después de la oleada 45 no aparece el panel.
- Los íconos de los modificadores activos se ven en el HUD.
- Al reiniciar la partida no queda ningún modificador aplicado.
