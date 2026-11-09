# 10 — Torretas nuevas (T1–T8)

GDD: 2.4 Turrets. El juego pasa de 5 a 13 torretas.

## Criterio de diseño

**Los tiles controlan y las torretas hacen daño.** Ninguna torreta nueva aplica slow ni stun: eso es trabajo de los tiles. Las torretas nuevas cubren huecos de daño o soporte y se apoyan en lo que hacen los tiles (las sinergias están en el MD 12).

## Estado actual (lo que se reusa)

- `Turret.cs`: arma el combate con `IShootingBehavior` + `ITargetingBehavior`. `RequiresCombatBehaviors(type)` solo exige shooter si `TurretDataSO.type` es `attack`, `aoe` o `slow`. Una torreta de soporte usa otro `type` (por ejemplo `support`) y no necesita shooter.
- `BasicProjectileShooter` + `Projectiles/Projectile.cs`: `Projectile` ya tiene `isAOE`, `explosionRadius` y `aoeLayers`, y aplica daño con `TurretDamageCalculator.Compute`.
- `BasicRaycastShooter`: daño instantáneo con `TurretDamageCalculator.ComputeInt`.
- `TurretStats`: `RecalculateStats()` multiplica base × nivel × modificadores × afinidad. Evento `OnStatsChanged`.
- `TurretDataSO`: `id`, `type`, `turretType` (`TurretType`), `displayName`, `icon`, stats, `affinity` / `affinityBonus` / `offWorldPenalty` y `prefab`.
- `TurretDatabaseSO` (`Assets/Scriptable Object/Turrets/Turrets/Database.asset`).
- Para el EMP y la destrucción de torretas: `Turret.DisableFor(seconds)` y `Enemies/Abilities/DIsableTowersEMP.cs` / `DestroyTurretOnLowHP.cs`.
- `WorldState.TimeInCurrentWorld` y `WorldState.OnWorldChanged`.
- Íconos: `Editor/TurretIconBaker.cs` (MD 08).

## Cambios generales

1. **`TurretType`**: agregar `Mortar, Ballista, RiftCannon, WarBanner, Purifier, Guillotine, Reaper, Warden`.
2. Por cada torreta:
   - un `TurretDataSO` en `Assets/Scriptable Object/Turrets/Turrets/`, con `id` en snake_case (`turret_mortar`, etc.);
   - un prefab en la carpeta de prefabs de torretas, armado a partir de un prefab existente (usar los modelos de `PolygonDungeonRealms` si hay catapulta, balista u otro modelo que sirva);
   - agregarla a `Database.asset`;
   - correr `Bake Turret Icons`.
3. Para cada script nuevo: `Turret/Variants/<Name>Turret.cs`, siguiendo la estructura de `FireTurret` / `TeslaTurret`. Todo daño pasa por `TurretDamageCalculator`, para que los modificadores O11 y O12 y las sinergias del MD 12 se apliquen.
4. Los stats base de la tabla son iniciales. Ponelos en los assets y no en el código.

## Las 8 torretas

| ID | Torreta | `type` | Afinidad | Base inicial |
| --- | --- | --- | --- | --- |
| T1 | Mortar | aoe | Normal | daño 14, rango 9, fireRate 0.35, radio de explosión 1.5, rango mínimo 2.5 |
| T2 | Ballista | attack | Neutral | daño 10, rango 11, fireRate 0.5, perforación ilimitada en línea |
| T3 | Rift Cannon | attack | Other | daño 6, rango 7, fireRate 1, +8% de daño por segundo en el mismo mundo, tope +200% |
| T4 | War Banner | support | Neutral | radio de aura 2.5 celdas, +15% de daño y +10% de cadencia a torretas aliadas |
| T5 | Purifier | attack | Normal | rayo continuo, 8 de daño por segundo, rango 7, x3 contra escudos |
| T6 | Guillotine | attack | Other | golpe del 8% de la vida máxima del objetivo (4% contra bosses), fireRate 0.25, rango 5 |
| T7 | Reaper | attack | Neutral | daño 5, rango 6, fireRate 1.2, sus kills dan +100% de esencias |
| T8 | Warden | support | Neutral | radio 2.5 celdas: protege a las torretas del EMP y de la destrucción |

### T1 — Mortar

- `BasicProjectileShooter` con un `Projectile` con `isAOE = true`.
- Trayectoria en arco: interpolar la posición con una parábola entre el disparo y el **punto** del objetivo al momento del disparo. Es un tiro al suelo y no persigue al enemigo. Agregar `arcHeight` y `targetGround` al `Projectile`, o crear `MortarProjectile` heredando la lógica de daño en área.
- **Rango mínimo**: crear `MinRangeTargeting` (un `ITargetingBehavior` que filtra enemigos a menos de `minRange`).
- Mostrar en el suelo el punto de impacto con un círculo que dura lo que dura el vuelo.

### T2 — Ballista

- Raycast con todos los impactos (`Physics.RaycastAll`) desde el `firePoint` hacia el objetivo, con largo = rango. Aplica daño a cada `Enemy` atravesado, con un 10% menos por cada enemigo previo (mínimo 50%).
- VFX: un proyectil visual rápido y una línea de trazado (reusar `TurretShotFeedback`).
- **Tiene que rendir según la forma del camino:** apunta al enemigo más adelantado y la perforación hace el resto. No hay que agregar lógica especial.

### T3 — Rift Cannon

- Disparo raycast (base `BasicRaycastShooter`).
- El daño se multiplica por `1 + min(maxBonus, WorldState.TimeInCurrentWorld * bonusPerSecond)`.
- Al dispararse `WorldState.OnWorldChanged`, el bonus vuelve a 0 con un VFX de "descarga".
- Indicador visual: un emisivo cuya intensidad sigue al bonus. El `TurretInfoPanel` muestra `"Rift charge +X%"`.

### T4 — War Banner

- No dispara.
- Crear `Turret/Variants/WarBannerAura.cs`: cada 0.5 s busca las `TurretStats` dentro del radio (excluyendo otros War Banner) y les registra un buff.
- **`TurretStats`**: agregar `AddExternalBuff(object source, float damageMult, float fireRateMult)` / `RemoveExternalBuff(object source)`. `RecalculateStats` los multiplica, y si una torreta tiene varios banners cerca, **se toma el mayor, no se suman**.
- El nivel del banner (duplicados) sube el buff: +3% de daño y +2% de cadencia por nivel.
- Al remover o destruir el banner, saca sus buffs.
- Visual: un aura en el suelo del radio y una línea fina hacia cada torreta buffeada mientras está seleccionado.

### T5 — Purifier

- Rayo continuo con `LineRenderer` hacia el objetivo, con daño por tick cada 0.2 s.
- **Escudos**: agregar `EnemyHealth.HasShield` (`true` si `shieldAuraApplied`, si está invulnerable por `Ability_LinkedShield` o si está activo `ShieldOnLowHP`; revisar cómo marca su estado cada habilidad). Contra enemigos con escudo, el daño es x3.
  - Si el escudo es la invulnerabilidad de `LinkedShield` o `ShieldOnLowHP`, el Purifier es **la única fuente** que puede hacer daño, al 100% sin el x3.
  - Si es el boost de vida de `ShieldAura`, cada tick reduce primero la vida extra.
  - Implementarlo con `EnemyHealth.TakeDamage(float amount, DamageFlags flags)`, con un flag `IgnoreInvulnerable`, sin romper la firma actual.
- Targeting por defecto: prioriza enemigos con escudo.

### T6 — Guillotine

- Golpe instantáneo con una animación de caída de hoja.
- Daño = `maxHealth * percent`, con `bossPercent` para `TurretDamageCalculator.IsBossType`. Pasa por `TurretDamageCalculator` para aplicar los modificadores.
- **El nivel sube el porcentaje** (+0.5% por nivel), no el daño base.
- Targeting por defecto: prioriza el enemigo con más vida máxima.

### T7 — Reaper

- Disparo raycast con poco daño.
- Si el golpe mata, el kill da +100% de esencias. Implementarlo marcando al enemigo con `Enemy.LastHitSource` (agregar campo y set en `TurretDamageCalculator` o en el shooter). En `WorldSwitchPoints`, si `LastHitSource` es un Reaper, multiplicar.
- El nivel sube el bonus (+10% por nivel).
- VFX: una guadaña fantasma breve en cada kill.

### T8 — Warden

- No dispara.
- `Turret/Variants/WardenAura.cs` registra en un `WardenRegistry` estático las torretas protegidas dentro del radio. El nivel sube el radio (+0.25 celdas por nivel).
- `DIsableTowersEMP` y `DestroyTurretOnLowHP`: antes de afectar una torreta, chequear `WardenRegistry.IsProtected(turret)`. Si está protegida, no le pasa nada y aparece un VFX de "bloqueado" en el escudo.
- El Warden también puede ser objetivo del EMP: no se protege a sí mismo, salvo que haya otro Warden cerca.

## Desbloqueo

El orden de desbloqueo está en el MD 13 (meta-progresión). Este MD solo crea las torretas y las suma a la base de datos.

## Criterios de aceptación

- Las 8 torretas se pueden elegir en el panel (temporalmente, hasta que el MD 13 filtre por mazo), colocar y subir de nivel con duplicados.
- Mortar: no dispara a enemigos pegados y hace daño en área en el punto de impacto.
- Ballista: un disparo daña a todos los enemigos de una fila.
- Rift Cannon: el daño crece mientras no cambiás de mundo y se resetea al cambiar.
- War Banner: las torretas cercanas muestran el buff en `TurretInfoPanel`. Dos banners no se suman.
- Purifier: puede dañar al Nigromante con `LinkedShield` activo. El resto de las torretas no.
- Guillotine: le saca un porcentaje visible de vida a un boss.
- Reaper: sus kills dan el doble de esencias (se ve en el orbe del MD 09).
- Warden: el Ángel Caído no desactiva ni destruye torretas dentro de su radio.
- Las 13 torretas tienen ícono.
