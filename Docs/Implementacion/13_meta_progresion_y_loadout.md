# 13 — Meta-progresión por XP y mazo (loadout)

Reemplaza al MD 07 (mejoras permanentes). Depende de los MD 10, 11 y 12, porque la tabla de desbloqueos referencia sus assets.

## Objetivo

1. **Meta-progresión**: desaparece la tienda de mejoras permanentes. Hay **una barra de XP por modo**:
   - **Normal**: se carga con las esencias azules ganadas en cada partida y desbloquea **tiles** (formas y efectos) y **modificadores del modo Normal**, incluidos los legendarios.
   - **Other World**: se carga con las esencias rojas y desbloquea **torretas** y **modificadores del modo Other**, incluidos los legendarios.
   - Las esencias **son** la XP. No se gastan en nada.
2. **Mazo**: antes de cada partida el jugador elige **de 3 a 5 torretas** y **de 3 a 5 tiles distintos** entre lo que tiene desbloqueado. En la partida, los paneles de opciones solo ofrecen cosas del mazo.

## Qué se elimina

Antes de borrar, buscar referencias en escenas y prefabs (mismo procedimiento que el MD 02) y dejarme un checklist para lo que haya que sacar a mano en el Editor.

- **Scripts**:
  - `Upgrades/`: `UpgradeSO`, `UpgradeService`, `UpgradeRepository`, `UpgradeLevels`, `PlayerPrefsUpgradeStateStore`, `UpgradeSystemBootstrap`, `UpgradeListUI`, `UpgradeDetailUI`, `TurretPermanentUpgrades` y `RunPermanentUpgrades`.
  - Restos del tope: `Tiles/TileLevelCapConfig.cs`, `TileLevelCapProvider.cs`, `TileLevelCapProviderSO.cs`, `ITileLevelCapProvider.cs`, `TileLevelingPermissionProvider.cs` e `ITileLevelingPermissionProvider.cs` (siguen en el proyecto aunque el MD 02 / 03 los sacaba).
- **Assets**: todo `Assets/Scriptable Object/Upgrades/`.
- **Usos a limpiar**:
  - `Turret/Data/TurretStats.cs` (llamada a `TurretPermanentUpgrades.ApplyPermanentUpgrades`).
  - `Core/Core.cs` (`RunPermanentUpgrades.CoreHealthBonus`).
  - `Rewards/RewardZoneSystem.cs` (línea ~361, `RunPermanentUpgrades.RewardZoneBonus`).
  - `SwitchMechanic/WorldSwitchPoints.cs` (`UpgradeLevels.Get` en `GetBlueMultiplier` / `GetRedMultiplier`, que pasan a usar solo modificadores).
  - `ShiftingWorldMechanic` (bonus de `upgrade_tileBarSpeed`, si quedó).
- **Menú**: los paneles Laboratory y Workshop se reemplazan por **Progression** y **Loadout**.

## Datos

1. **`Progression/UnlockTableSO.cs`** (`CreateAssetMenu "Progression/Unlock Table"`):
   - `List<UnlockLevel> normalLevels`, `List<UnlockLevel> otherLevels`.
   - `UnlockLevel { int level; List<TileLayout> tiles; List<TurretDataSO> turrets; List<ModifierSO> modifiers; }`.
   - `List<TileLayout> startingTiles`, `startingTurrets`, `startingNormalModifiers` y `startingOtherModifiers`.
   - Asset en `Assets/Scriptable Object/Progression/UnlockTable.asset`.
2. **`Progression/XpCurveSO.cs`**: `xpToNext(level) = Mathf.RoundToInt(baseXp * Mathf.Pow(level, exponent))`, con `baseXp = 100` y `exponent = 1.4`, configurables.
3. **`Progression/ProgressionStore.cs`** (clase estática, `PlayerPrefs`):
   - XP total y nivel por modo.
   - `AddXp(World mode, int amount)` devuelve la lista de niveles ganados.
   - `IsUnlocked(TileLayout)`, `IsUnlocked(TurretDataSO)`, `IsUnlocked(ModifierSO)`.
   - `GetUnlockedTiles()`, `GetUnlockedTurrets()`, `GetUnlockedModifiers(ModifierCategory)`.
   - **Migración**: si existen las keys viejas de `EssenceBank` (`TotalBlueEssences`, `TotalRedEssences`), convertirlas en XP una sola vez y borrarlas.
   - `EssenceBank` se elimina o queda como fachada de `ProgressionStore`. Elegí lo que menos rompa y avisame.
4. **`Progression/LoadoutStore.cs`**: guarda el último mazo (IDs de layouts por `name` y de torretas por `id`). Al cargar, descarta lo que ya no está desbloqueado o no existe y completa con lo inicial hasta llegar al mínimo de 3.

## Tabla de desbloqueos (valores iniciales)

Los tiles espejados (`mirrorOf`, MD 11) no figuran: se desbloquean junto con su original.

**Inicio**:
- Tiles: `Tile-Up`, `Tile-Left`, `Tile-UpDPS`.
- Torretas: Basic, Fire, Tesla.
- Modificadores Normal: N01, N02, N04, N09, N13.
- Modificadores Other: O01, O02, O03, O04, O10.

### Barra Normal

| Nivel | Tiles | Modificadores |
| --- | --- | --- |
| 2 | `Tile-UpSlow` | N05 |
| 3 | `Tile-Up-Left`, `Tile-LeftDPS` | — |
| 4 | `Tile-UpStun` | N06 |
| 5 | `Tile-Serpent` | N03 |
| 6 | `Tile-LeftSlow`, `Tile-LeftStun` | — |
| 7 | `Tile-Plaza` | N10 |
| 8 | `Tile-DeadEnd` | N14 |
| 9 | `Tile-Hybrid-SlowDps`, `Tile-ForkT` | N07 |
| 10 | — | **LN1** |
| 11 | `Tile-UpDPS-Normal`, `Tile-Up-LeftDPS` | N08 |
| 12 | `Tile-UpExposure`, `Tile-Up-LeftSlow` | N11 |
| 13 | `Tile-Hybrid-StunDps`, `Tile-Up-LeftStun` | N12 |
| 14 | `Tile-LeftAntiHeal`, `Tile-alldirections` | N15 |
| 15 | `Tile-alldirectionsDPS`, `Tile-alldirectionsSlow` | **LN2** |
| 16 | `Tile-UpSlow-Other`, `Tile-alldirectionsStun` | **LN3** |

### Barra Other World

| Nivel | Torretas | Modificadores |
| --- | --- | --- |
| 2 | Sniper | O05 |
| 3 | Mortar | — |
| 4 | Ballista | O11 |
| 5 | Energy | O13 |
| 6 | War Banner | O06 |
| 7 | Guillotine | O07 |
| 8 | Purifier | O08 |
| 9 | Rift Cannon | O12 |
| 10 | — | **LO1** |
| 11 | Reaper | O09 |
| 12 | Warden | O14 |
| 13 | — | O15, **LO2** |
| 14 | — | **LO3** |

Si algún asset de la tabla no existe con ese nombre, avisame en vez de reemplazarlo.

**Calibración de la curva**: con `WaveList.asset` y los `essenceReward`, calculá las esencias azules + rojas esperadas al morir en la oleada 15, en la 30 y al ganar (suponiendo 50/50 de tiempo en cada modo). Pasame una tabla con la cantidad de partidas necesarias para llegar al nivel 10 y al nivel máximo de cada barra. La meta: nivel 10 en unas 8 partidas y todo desbloqueado en unas 20. Ajustá `baseXp` / `exponent` según eso y mostrame los valores antes del commit.

## Juego: usar el mazo

1. **`Progression/RunLoadout.cs`** (estático): `Tiles` y `Turrets` de la partida actual. Lo llena el menú al apretar Start y lo lee GameScene.
   - Si se entra a GameScene sin pasar por el menú (Editor), usar `LoadoutStore` o, si no, lo inicial.
2. **`GridGenerator`**: agregar `SetCandidateLayouts(IEnumerable<TileLayout>)`. En `Start`, antes de `GenerateFirst`, usar `RunLoadout.Tiles`. `initialLayout` no cambia.
   - **Respaldo obligatorio**: si `GetRandomCandidateSet` no encuentra ningún layout del mazo que entre en una salida, agregar al set `Tile-Up` y `Tile-Left` (estén o no en el mazo) para que el juego nunca se trabe. Loguear un warning `[GridGenerator]`.
3. **`ShiftingWorldUI.BuildOtherOptions()`**: el pool de torretas es `RunLoadout.Turrets`, no `turretDatabase` ni `turretPool`.
   - Con menos opciones en el mazo que botones (por ejemplo O02 con un mazo de 3), mostrar solo las distintas. **No repetir la misma torreta en dos botones**: hoy `pool[i % pool.Count]` lo permite.
4. **Modificadores**: `GameModifiersManager.GetAvailable` (y los pools legendarios) filtra por `ProgressionStore.IsUnlocked`.
5. **Recompensas** (`RewardZoneSystem`, `BountyMarker`): el duplicado gratis sale solo de cosas del mazo que estén colocadas.

## Fin de partida: XP

1. `GameManager`, al terminar la partida (ganada o perdida), llama a `ProgressionStore.AddXp(Normal, totalBlueEssences)` y `AddXp(Other, totalRedEssences)`, y guarda los niveles ganados en `RunStats` (MD 09) para `ResultScene`.
2. **`ResultSceneController`**:
   - las dos barras se llenan animadas, en 1.5 s, con tiempo unscaled;
   - en cada level up, un flash y un SFX;
   - debajo, cards con lo desbloqueado: ícono, nombre y `"New tile"` / `"New turret"` / `"New modifier"`.
   - Si se desbloqueó un legendario, va con marco dorado.

## Menú

1. **Panel Progression** (reemplaza Laboratory / Workshop):
   - Las dos barras con nivel y XP (`"Lv 4 — 230 / 480"`).
   - Debajo de cada barra, una "ruta": los próximos 3 niveles con sus desbloqueos, con siluetas y el texto `"Lv X"`.
   - Si existe `CameraPanelManager` / `PanelSwitcher`, reusarlos para la navegación.
2. **Panel Loadout**: se abre al apretar Play, **antes** de la animación de `PlayButtonController.OnPlayButtonClick`.
   - Dos columnas, **Tiles** y **Turrets**. Cada una tiene una grilla de cards con todo lo desbloqueado (íconos de `TileIconGenerator` y `TurretDataSO.icon`) y, arriba, 5 slots seleccionados.
   - Click en una card: entra o sale del mazo. Máximo 5 por columna; no se puede repetir.
   - Las cards bloqueadas se ven como silueta con un candado y `"Lv X"`, no se pueden elegir y tienen tooltip.
   - Tooltip en cada card (reusar `OptionTooltip`): efecto, forma, stats y la sinergia del MD 12.
   - Botón **Start**: solo activo con 3 a 5 por columna. Guarda en `LoadoutStore`, setea `RunLoadout` y dispara la animación y la carga de escena actuales.
   - Botón **Back**.
   - Los modificadores no se eligen: entran todos los desbloqueados.

## Documentación

- Actualizar `00_PROMPT.md`: marcar `07_mejoras_permanentes.md` como reemplazado por este MD.
- Actualizar el tutorial (`TutorialManager`): agregar un paso para el panel Loadout la primera vez que se juega.

## Criterios de aceptación

- No queda ninguna referencia a `UpgradeSO`, `UpgradeLevels`, `RunPermanentUpgrades` ni `TurretPermanentUpgrades`. No hay "Missing Script" en ninguna escena.
- Un jugador nuevo arranca con 3 tiles, 3 torretas y 5 modificadores por modo, y el mazo viene armado con eso.
- En la partida solo salen tiles y torretas del mazo. Nunca aparece la misma torreta en dos botones a la vez.
- Si ningún tile del mazo entra en ninguna salida, aparece el respaldo y el juego no se traba.
- Al terminar la partida, las barras suben con las esencias de cada color y los desbloqueos se muestran en `ResultScene`.
- Lo desbloqueado aparece en el Loadout la próxima vez, y los modificadores nuevos aparecen en el panel.
- Me pasaste la tabla de calibración de la curva de XP.
