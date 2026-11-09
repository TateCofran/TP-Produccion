# 11 — Variantes de tiles (posicionamiento y efectos)

GDD: 2.5 Path Building. Incluye el punto 12 (Fork y Dead End).

Variantes incluidas: **P1 Fork, P2 Dead End, P3 Serpentina, P4 Plaza, E1 Híbridos, E2 World-locked, E3 Exposure y E5 Anti-heal.**

## Estado actual

- `Tiles/TileLayout.cs`: grilla de `gridWidth × gridHeight` (5×5), `TileType { Grass, Path }`, `entry`, `exits` (lista), `PathModifiers { dps, slow, stun }` por defecto y `perCellOverrides` por celda.
- `Editor/TileLayoutEditor.cs`: editor visual de layouts.
- `Tiles/Services/LayoutInstatiator.cs`: elige el prefab de cada celda con `IPrefabSelector.ChooseForMods(dps, slow, stun)` y llama a `PathCellEffect.Setup`.
- `Tiles/Pooling/PrefabSelector.cs`: `Grass`, `PathBasic`, `PathDamage`, `PathSlow` y `PathStun`.
- `Tiles/PathCellEffect.cs` + `Tiles/PathEffectManager.cs`: aplican el efecto. En el Other World, Slow hace daño y Stun empuja al enemigo hacia atrás (`IsWorldFlipped`).
- `Enemies/EnemyEffects.cs`: slow, stun, DoT y daño instantáneo.
- `Enemy.IsTargetable()` devuelve `data == null || WorldState.Matches(data.vulnerableIn)`.
- **Los enemigos spawnean en las salidas abiertas**: `Tiles/Services/SpawnPointService.cs`, `GetSpawnPoints()` → `_exits.GetAvailableWorld`. Si no hay ninguna, usa como respaldo las salidas del último tile.
- Assets existentes en `Assets/Scriptable Object/Tiles/{Basic,Dps,Slow,Stun}/`: `Tile-Up`, `Tile-Left`, `Tile-Right`, `Tile-Up-Left`, `Tile-Up-Right` y `Tile-alldirections` (más `Tile-Initial` en Basic).
- `Tiles/UI/TileIconGenerator.cs` + `TileIconPalette.asset`: íconos (MD 08). `TileOptionButton`: tooltip.

## Antes de empezar: tiles espejados

Con `allowFlip = true` en `GridGenerator`, `Tile-Left` y `Tile-Right`, igual que `Tile-Up-Left` y `Tile-Up-Right`, son la misma pieza espejada. En el mazo del MD 13 eso confunde: el jugador elegiría "dos" tiles que son uno.

- Verificá con `TileIconGenerator` que de verdad sean espejos.
- Si lo son, **no los borres**. Agregá en `TileLayout` un campo `[SerializeField] private TileLayout mirrorOf;`. Los layouts con `mirrorOf` asignado no aparecen en el mazo ni en la tabla de desbloqueos, y el dupe system cuenta sus copias para el original (clave = `mirrorOf ?? this`).
- Pasame la lista de pares antes de hacer el commit.

## P1 — Fork

- Los `Tile-alldirections*` ya tienen varias salidas. Confirmá cuántas tiene cada uno.
- Crear además **Fork-T**: entrada abajo y 2 salidas (izquierda y derecha), en Basic. Asset: `Assets/Scriptable Object/Tiles/Special/Tile-ForkT.asset`.
- **Regla de diseño**: cada salida abierta es un carril de spawn. El tooltip de los Fork tiene que decir `"+1 enemy lane"` por cada salida extra (calcularlo como `exits.Count - 1`).
- Verificar que `SpawnManager` reparta los enemigos entre todos los spawn points (round robin con `GetNextRoundRobin`).

## P2 — Dead End

Layout `Tile-DeadEnd` en `Special/`, con un camino de 3 celdas desde la entrada y **sin salidas**:

```
y4  . . . . .
y3  . . . . .
y2  . . # . .
y1  . . # . .
y0  . . E . .
```

- **`GridGenerator`**: un layout sin exits tiene que poder colocarse. Revisá `PlaceTile`, `AddTileExitsToPool` y `ResolveBlockedExits` para que no fallen con una lista vacía.
- **No se puede cerrar la última salida**: si colocarlo dejaría 0 salidas abiertas, `LayoutFitsAnyExit` devuelve `false` para ese layout. Así nunca se ofrece ni se coloca en ese caso.
- **Recompensa**: agregar en `TileLayout` `public int onPlaceEssenceReward;` (Dead End = 15). Al colocarlo, dar esa cantidad de esencia del modo activo con `WorldSwitchPoints.AddBlueEssence` / `AddRedEssence`, con orbe (MD 09).
- Tooltip: `"Closes this lane, +15 essences"`.

## P3 — Serpentina

`Tile-Serpent` en `Special/`, Basic, con un camino de 9 celdas y poco pasto:

```
y4  . X . . .
y3  . # # # .
y2  . . . # .
y1  . # # # .
y0  . E . . .
```

## P4 — Plaza

`Tile-Plaza` en `Special/`, Basic, con un camino recto pegado al borde y el resto pasto para torretas:

```
y4  X . . . .
y3  # . . . .
y2  # . . . .
y1  # . . . .
y0  E . . . .
```

Para P2, P3 y P4: respetá la convención de entrada y salida de `Tile-Up` (borde y celda). Si la entrada de `Tile-Up` no está en `y0`, adaptá los dibujos manteniendo la forma.

## E1 — Híbridos

Sin código nuevo: se arman con `perCellOverrides`.

- **`Tile-Hybrid-SlowDps`**: forma `Tile-Up`, las 2 primeras celdas desde la entrada en Slow (0.3) y el resto en DPS (mismo valor que `Tile-UpDPS`).
- **`Tile-Hybrid-StunDps`**: forma `Tile-Left`, la celda de entrada en Stun (mismo valor que `Tile-UpStun`) y el resto en DPS.

**Ajustes que sí hacen falta:**
- `TileIconGenerator` tiene que pintar cada celda según su override (verificar).
- `TileOptionButton` muestra en el tooltip los dos efectos.
- `TileLevelApplier` (MD 03) escala todos los efectos de las celdas.

## E2 — World-locked

- `TileLayout`: agregar `public WorldAffinity activeIn = WorldAffinity.Neutral;` y `public float lockedStrengthMultiplier = 2f;`.
- `PathCellEffect`: si `activeIn != Neutral` y `!WorldState.Matches(activeIn)`, la celda no aplica ningún efecto. Si coincide, los valores efectivos se multiplican por `lockedStrengthMultiplier`. Pasar el dato en `LayoutInstantiator.SetupPathCell`.
- Visual: con el mundo que no corresponde, las celdas se ven desaturadas (`MaterialPropertyBlock`). El ícono lleva una marca del mundo.
- Assets: `Tile-UpDPS-Normal` (DPS, solo Normal) y `Tile-UpSlow-Other` (Slow, solo Other: o sea su versión invertida de daño x2).

## E3 — Exposure

- `TileLayout.PathModifiers`: agregar `public bool exposure;`.
- `EnemyEffects`: `public bool IsExposed => _exposedUntil > Time.time;` y `ApplyExposure(float duration)`.
- `PathCellEffect`: mientras el enemigo está en la celda, refresca `ApplyExposure(0.5f)`. Respeta `EffectLingerSeconds` (N08).
- `Enemy.IsTargetable()`: `return data == null || WorldState.Matches(data.vulnerableIn) || (_effects != null && _effects.IsExposed);`.
- Visual del enemigo expuesto: un outline o marca (reusar `EnemyPhaseVisual` para que se vea como "en fase").
- `PrefabSelector` / `IPrefabSelector`: agregar `PathExposure` y adaptar `ChooseForMods` (con una sobrecarga que reciba los flags) y `GridGenerator`, que tiene los `[SerializeField]` de los prefabs. Crear el prefab de celda copiando `PathSlow` y cambiando el material.
- Asset: `Tile-UpExposure`.

## E5 — Anti-heal

- `PathModifiers`: `public bool antiHeal;`.
- `EnemyEffects`: `IsHealBlocked`, `ApplyHealBlock(float duration)`. Aplicarlo al pasar por la celda, con una duración de 4 s.
- `EnemyHealth.Heal(float amount)`: no cura si el enemigo tiene `EnemyEffects.IsHealBlocked`. Eso corta `Ability_HealAura` y `Ability_SelfHealPeriodic`.
- Visual: una partícula oscura sobre el enemigo mientras dure el bloqueo.
- `PrefabSelector`: `PathAntiHeal`. Asset: `Tile-LeftAntiHeal`.

## Paleta e íconos

En `TileIconPalette` agregar colores para Exposure (violeta), Anti-heal (verde oscuro) y una marca de mundo para los World-locked. Agregar un ícono distinto para el Dead End (una X en el final).

## Criterios de aceptación

- Los espejos están marcados y cuentan como el mismo tile para los duplicados.
- Un Fork abre dos ramas y aparecen enemigos en ambas.
- El Dead End cierra la rama, da 15 esencias, y no aparece como opción si cerraría la última salida.
- La Serpentina y la Plaza se colocan bien en las 4 rotaciones y con flip.
- Los híbridos aplican dos efectos distintos en distintas celdas, y el ícono lo muestra.
- El tile World-locked no hace nada en el mundo contrario, hace el doble en el suyo y se ve desaturado cuando está inactivo.
- Con Exposure, una torreta puede atacar a un enemigo que no es vulnerable en el mundo actual mientras está sobre el tile.
- Con Anti-heal, el Ojo Maligno no cura a los enemigos que pasaron por el tile durante 4 s.
