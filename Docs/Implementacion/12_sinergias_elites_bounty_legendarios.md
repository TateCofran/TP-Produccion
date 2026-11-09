# 12 — Sinergias, élites, bounty y modificadores legendarios

Depende de los MD 10 (torretas nuevas) y 11 (variantes de tiles). Implementar por bloques A → D, con un commit por bloque.

---

## A. Sinergias entre torretas y tiles

### Objetivo

Una torreta colocada en una celda de pasto **adyacente** (8 vecinos, en el mismo tile o en uno vecino) a una celda de camino con cierto efecto gana un bonus. Hace que importe dónde se construye el camino respecto de las torretas.

### Tabla (valores iniciales)

| Torreta | Efecto vecino | Bonus |
| --- | --- | --- |
| Basic | DPS | +15% de cadencia |
| Fire | DPS | el burn dura +50% |
| Tesla | Stun | +30% de daño a enemigos aturdidos |
| Sniper | Slow | +30% de daño a enemigos ralentizados |
| Energy | cualquier efecto | +20% de energía |
| Mortar | Slow o Stun | +25% de radio de explosión |
| Ballista | Slow | +25% de daño |
| Rift Cannon | World-locked de su mundo (Other) | el bonus crece x1.5 más rápido |
| War Banner | cualquier efecto | +1 celda de radio de aura |
| Purifier | Exposure | x2 de daño |
| Guillotine | Stun | porcentaje de vida x1.5 |
| Reaper | Anti-heal | +50% de bonus de esencias |
| Warden | Stun | +50% de radio de protección |

### Cambios

1. **`Assets/Scripts/Synergy/SynergyTableSO.cs`** (`CreateAssetMenu "Synergy/Table"`):
   - Lista de `SynergyRule { TurretType turret; TileEffectKind requiredEffect; SynergyStat stat; float value; }`.
   - `TileEffectKind`: `Dps, Slow, Stun, Exposure, AntiHeal, WorldLockedSameWorld, Any`.
   - `SynergyStat`: `FireRate, Damage, BurnDuration, DamageVsStunned, DamageVsSlowed, EnergyOutput, AoeRadius, RiftChargeRate, AuraRadius, PercentDamage, EssenceBonus, ProtectionRadius`.
   - Asset en `Assets/Scriptable Object/Synergy/SynergyTable.asset`, con la tabla de arriba.
2. **`Synergy/SynergyService.cs`** (MonoBehaviour en GameScene):
   - `GetActiveSynergies(CellSlot slot, TurretType type)` mira las celdas de camino vecinas de `slot` y devuelve las reglas que se cumplen.
   - Para saber el efecto de una celda vecina: `PathCellEffect` expone `public TileEffectKind[] Kinds` (calculado en `Setup`) y `WorldAffinity ActiveIn` (MD 11). Buscar vecinos con `Physics.OverlapBox` en la layer del camino, con un tamaño de 1.5 × `cellSize`.
   - Recalcula al colocar una torreta (`CellSlot.TryPlace`) y en cada `GridGenerator.OnTilePlaced`, porque un tile nuevo puede activar sinergias de torretas ya colocadas.
3. **`Synergy/TurretSynergy.cs`** (componente que se agrega a cada torreta al colocarla):
   - Guarda las sinergias activas.
   - `TurretStats` las lee en `RecalculateStats` (FireRate, Damage, AoeRadius, AuraRadius, ProtectionRadius), y cada variante lee las suyas: `FireTurret` BurnDuration, `TurretEnergy` EnergyOutput, Rift ChargeRate, Guillotine PercentDamage, Reaper EssenceBonus.
4. **Condicionales de daño** (`DamageVsStunned` / `DamageVsSlowed`): en `TurretDamageCalculator.Compute`, agregar un parámetro opcional `TurretSynergy source` y chequear `target` con `EnemyEffects.IsStunned` / `EnemyEffects.CurrentSpeedMultiplier < 1`.
5. **UI**:
   - El fantasma de torreta del MD 08 (`TurretGhostPreview`) muestra, sobre la celda hovereada, los íconos de las sinergias que se activarían, con un brillo en las celdas de camino que las causan.
   - El `TurretInfoPanel` lista las sinergias activas.
   - El tooltip de `TurretOptionButton` muestra `"Synergy: next to <effect> → <bonus>"`.

---

## B. Enemigos élite con afijos

### Objetivo

Desde la zona 2, algunos enemigos comunes aparecen como élite: con un afijo al azar, un aura de color y el triple de esencias.

### Cambios

1. **`Enemies/Elite/EliteAffix.cs`**: `enum EliteAffix { Hasted, Armored, Regenerating, Shielded, Splitter }`.
2. **`Enemies/Elite/EliteController.cs`** (agregarlo a todos los prefabs de enemigos; si no se puede editar cada prefab, que `EnemyPool` lo agregue al crear la instancia):
   - `Apply(EliteAffix affix)` y `Clear()`. `Clear()` se llama desde `Enemy.Init`.
   - **Hasted**: `EnemyEffects.AddSpeedMultiplier(this, 1.35f)`.
   - **Armored**: `EnemyHealth.SetDefense(defensa actual + 3)`.
   - **Regenerating**: cura el 2% de la vida máxima por segundo. Respeta el anti-heal del MD 11.
   - **Shielded**: `EnemyHealth.TryApplyShieldAuraBoost(1.5f)`. Cuenta como escudo para el Purifier.
   - **Splitter**: al morir, spawnea 2 minions del mismo zona con `MarkAsSummoned()` (copiar la lógica de `Ability_SpawnOnDeath`).
   - Visual: un aura en el suelo del color del afijo y un prefijo en el nombre (`"Hasted Skeleton"`) en `EnemyPopupUI` / `EnemyInfoPanel`.
   - `public bool IsElite`.
3. **`Enemies/Elite/EliteSpawnRules.cs`** (ScriptableObject):
   - Probabilidad por zona: zona 1 = 0%, zona 2 = 6%, zona 3 = 12%.
   - Pesos por afijo.
   - Tipos excluidos: MiniBoss* y Boss*.
4. **`SpawnManager.SpawnOne`**: después de `enemy.Init(route)`, tirar la probabilidad (la zona se calcula como `(wave - 1) / 15 + 1`). Si sale élite, `eliteController.Apply(...)`. Los summoned nunca son élite.
5. **`WorldSwitchPoints.HandleEnemyKilled`**: si el enemigo es élite, `essenceReward × 3`.

---

## C. Enemigo bounty

### Objetivo

Muy de vez en cuando aparece un enemigo dorado que va más rápido, no daña al core y, si lo matás, da una recompensa grande.

### Cambios

1. **`Enemies/Bounty/BountySpawner.cs`** (MonoBehaviour en GameScene):
   - En `WaveManager.OnWaveStarted`, desde la oleada 4 y fuera de las oleadas de boss (15, 30, 45), un 15% de probabilidad (configurable) de spawnear 1 bounty a mitad de la oleada.
   - Lo saca del pool del tipo Sprint de la zona actual y lo inicializa como cualquier enemigo. Llama a `WaveManager.RegisterExtraEnemies(1)`.
2. **`Enemies/Bounty/BountyMarker.cs`** (se agrega o habilita en ese enemigo):
   - Vida x2, velocidad x1.4, material dorado con brillo y partículas.
   - Al llegar al core no hace daño: hay que agregar en `Enemy.HitCore` un chequeo `if (bounty) core damage = 0`. Además, el enemigo "escapa" con un VFX y aparece el toast `"Bounty escaped"`.
   - Al morir (`OnAnyEnemyKilled` con `!ReachedCore`), da la recompensa con la misma lógica que `RewardZoneSystem.Grant` (reusarla, sin duplicarla): 60% 30 esencias del modo activo, 40% un duplicado gratis de algo del mazo que esté colocado. Toast `"Bounty: ..."`.
   - Nunca es élite.
3. **UI**:
   - Toast `"Bounty spotted!"` al aparecer.
   - Flecha en el borde de la pantalla apuntando al bounty si está fuera de cámara (`Feedback/OffscreenIndicator.cs`).
   - SFX propio.

---

## D. Modificadores legendarios

### Objetivo

Después de vencer al boss de la zona 1 (oleada 15) y al de la zona 2 (oleada 30), aparece un panel con **hasta 2 legendarios del modo activo**, desbloqueados (MD 13) y no aplicados. El jugador elige uno. Los legendarios rompen una regla del juego.

### Los 6 legendarios

| ID | Categoría | Nombre | Efecto | Implementación |
| --- | --- | --- | --- | --- |
| LN1 | LegendaryNormal | Unified Ground | Los tiles con el mismo efecto cuentan como duplicados entre sí | `TileDupeSystem`: la clave pasa a ser el efecto principal del layout mientras el stat esté activo. Al activarlo, sumar los duplicados existentes por efecto |
| LN2 | LegendaryNormal | Cartographer | Se revela la niebla en 2 tiles de radio alrededor de todo el camino, todas las zonas de recompensa quedan visibles y sus recompensas se duplican | `FogOfWarSystem.RevealRadius(...)` + stats `RewardZoneAlwaysVisible` / `RewardZoneBonus` |
| LN3 | LegendaryNormal | Primal Ground | Los tiles aplican su efecto normal **y** su efecto invertido en ambos mundos | `PathEffectManager`: aplicar los dos comportamientos (`IsWorldFlipped` y no flipped) |
| LO1 | LegendaryOther | Worldless | Las torretas siempre reciben su `affinityBonus` y nunca su `offWorldPenalty` | `TurretStats.AffinityMultiplier` |
| LO2 | LegendaryOther | Rift Walker | El cooldown del cambio de mundo baja un 80% | `ShiftingWorldMechanic.toggleCooldownSeconds × (1 - stat)` |
| LO3 | LegendaryOther | Arsenal | Cada torreta colocada suma 1 copia a **todos** los tipos de torreta colocados | En `TurretDupeSystem.AddDupe`, si el stat está activo, `AddDupe` también para los otros tipos colocados (sin recursión) |

### Cambios

1. **`ModifierStat`**: agregar `UnifiedGround, Cartographer, PrimalGround, Worldless, ShiftCooldownReduction, Arsenal`.
2. Crear los 6 `ModifierSO` en `Assets/Scriptable Object/Modifiers/Legendary/`, con `category` `LegendaryNormal` / `LegendaryOther`. Extender `Editor/ModifierAssetCreator.cs` para que los cree.
3. **`GameModifiersManager`**: agregar los pools `legendaryNormalPool` / `legendaryOtherPool` y extender `GetAvailable` para esas categorías. `CategoryForCurrentWorld` tiene que poder devolver la versión legendaria.
4. **`Modifiers/LegendaryTrigger.cs`**: en `WaveManager.OnWaveEnded`, si la oleada es 15 o 30, encolar con `ModalPanelQueue` y una **prioridad mayor que la del modificador común**. Las oleadas 15 y 30 también son múltiplos de 3, así que se abren los dos paneles uno detrás del otro: primero el legendario. Usar `ModifierPanelSelection` con las cards legendarias (borde dorado, partículas). Si hay 0 legendarios disponibles, no abre nada.
5. **`ActiveModifiersHUD`**: los legendarios se muestran primero y con marco dorado.

## Criterios de aceptación

- **A**: una Tesla al lado de un tile Stun muestra la sinergia en el fantasma y en el `TurretInfoPanel`, y hace +30% de daño a enemigos aturdidos. Colocar un tile Stun al lado de una Tesla ya colocada activa la sinergia.
- **B**: en la zona 2 aparecen élites con aura y prefijo. Dan el triple de esencias. Los bosses y los summoned nunca son élite.
- **C**: el bounty aparece con toast y flecha. Si llega al core no hace daño. Si muere, da su recompensa.
- **D**: después de la oleada 15 aparece primero el panel legendario y después el común. Cada legendario cambia la regla que dice la tabla.
