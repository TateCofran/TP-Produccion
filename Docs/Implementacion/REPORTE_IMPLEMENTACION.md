# Reporte de implementación — MD 01 a 13

Última actualización: 2026-11-06. Rama `main`, un commit por MD (el MD 09, uno por bloque). **Ningún commit está pusheado.** El seguimiento por tarea está también en Notion: base **Backlog de implementación** (dentro de "Shadow Realms — Documentación").

| MD | Commit | Estado |
|---|---|---|
| 01 Esencias por kill | `8a31777` | ✅ Hecho. Probado en Play Mode |
| 02 Sin tope de nivel | `d19587a` | ✅ Hecho (curva pendiente de decisión) |
| 03 Duplicados de tiles | `4da6a65` | ✅ Hecho (tiles Basic pendientes de decisión). Probado en Play Mode |
| 04 Modificadores | `e098157` + O09 `cc92285` | 🟡 Lógica hecha. Falta la UI en el Editor. O09 resuelto (+30% energía) |
| 05 Niebla y zoom | `5e8d039` | 🟡 Hecho. **Conflicto con el zoom máximo** |
| 06 Zonas de recompensa | `f8bb878` + fix `0b922fa` | ✅ Hecho. El fix asigna la `RewardTable`, que había quedado vacía |
| 07 Mejoras permanentes | `de36b87` | ❌ Reemplazado y eliminado por el MD 13 (`0465fd2`) |
| 08 Previews en selección | `6bea087` | ✅ Hecho. Probado en Play Mode |
| 09 A Riesgos técnicos | `7a8a9c9` + `2805fad` | ✅ Hecho. **Revisá los textos del tutorial** (abajo). `TutorialManager` reactivado en `2805fad` |
| 09 B Economía y decisiones | `b94514f` | ✅ Hecho. Probado en Play Mode |
| 09 C Combate | `3631ad7` | ✅ Hecho. Probado en Play Mode. Incluye el arreglo pendiente del Progenitor |
| 09 D Core y cámara | `ef4a2b6` | ✅ Hecho. Probado en Play Mode |
| 09 E Features nuevas | `7bbdbdb` | ✅ Hecho. Probado en Play Mode. Disolve de niebla con alternativa (ver decisiones) |
| 09 F Opcional | — | ⏸️ No hecho: el MD dice que solo se hace si lo pedís. El punto 26 además está pendiente de decisión |
| 10 Torretas nuevas | `cd725a7` | ✅ Hecho. Probado en Play Mode. Modelos provisorios (ver MD 10) |
| 11 Variantes de tiles | `7262457` | ✅ Hecho. Probado en Play Mode |
| 12 A Sinergias | `6ba8c17` | ✅ Hecho. Probado en Play Mode |
| 12 B+C Élites y bounty | `5caa7d3` | ✅ Hecho. Probado en Play Mode (un solo commit: comparten `Enemy.cs`) |
| 12 D Legendarios | `083d70c` | ✅ Hecho. Probado en Play Mode salvo Primal Ground. Necesita el panel de modificadores del MD 04 para verse en el juego real |
| 13 Meta-progresión y loadout | `0465fd2` | ✅ Hecho. Probado en Play Mode. Curva aprobada (20 / 2) |

**Verificación general:**
- Compila sin errores después de cada MD y cada bloque.
- Los únicos warnings de compilación son los 9 `CS0414` que ya estaban antes de empezar.
- 0 scripts faltantes en las 4 escenas y en todos los prefabs.
- Desde el MD 08 las pruebas se hacen en **Play Mode**, entrando desde el Menú (porque `SoundManager` vive en Menu.unity). Antes de eso las pruebas fueron en modo Edit; los MD 01, 03, 04, 05 y 06 se repasaron después en Play Mode.
- Para que el Editor avance frames con la ventana en segundo plano, durante las pruebas puse `Application.runInBackground = true` **solo en runtime** (no queda guardado).
- Al probar el tutorial puse la PlayerPref `Tutorial_Completed = 1` en esta máquina.
- En Play aparecen 2 avisos `The referenced script (Unknown) on this Behaviour is missing!` al cargar GameScene. No hay scripts faltantes en escenas, prefabs, controllers ni assets; no encontré el origen.

Quedan sin commitear cosas que no tocan estos MD:
- `Fantasy Meter.mat` y `M_FillRed.mat` (los modifica Unity en runtime);
- `Packages/`, `Docs/`, `GDD_Revision.md`, `.mcp.json`, `Assets/Plugins/` y `ProjectSettings/Packages/`.

---

## Tus decisiones pendientes

1. **MD 02, curva de nivel:** queda lineal. El único punto de cambio es `TurretLevelData.GetLevelBonus(level)`, en `Turret/DupeSystem/ITurretDupeSystem.cs`.
2. **MD 03, tiles Basic:** subirlos de nivel no cambia nada (no tienen efecto), pero el nivel se muestra igual. Con el modificador N07 (Toxic Trail), hacen daño fijo; ese daño no escala con el nivel.
3. **MD 04, O09 Power Grid (conflicto):** el MD pide multiplicar "el radio del efecto" de la torreta Energy. `TurretEnergy` **no tiene radio**: suma energía por segundo a la barra global de modos, sin área.
   - **Resuelto:** O09 ahora da +30% de energía generada. El stat se renombró a `EnergyOutput`, `TurretEnergy.GetChargePerSecond()` lo multiplica y O09 se agregó al `otherPool` (15 modificadores).
4. **MD 05, zoom máximo (conflicto):** la cámara está a 10 de altura con 45° de inclinación y `maxZoom` = 70. La grilla de niebla con `radiusInTiles` 8 mide unos ±42 unidades. Solo con FOV es imposible verla entera: haría falta un FOV de más de 150°.
   - Dejé `minZoom`/`maxZoom` sin tocar.
   - **Opciones:** (a) un zoom que también suba la cámara en Y; (b) bajar `radiusInTiles` a unos 3 o 4; (c) aceptar que con zoom out se ven solo las zonas cercanas.
5. **MD 05, plano de niebla viejo:** el objeto `Fog` de GameScene es un cubo de 400×400 en y = -0.5, con `BoxCollider` y `FogWorldMaterialSwitcher`. Funciona como fondo, por debajo de los tiles, y no queda redundante con los fog tiles. Lo dejé tal cual. ¿Lo saco?
6. **MD 07, costos:** no cambié ningún costo existente (tabla abajo). La mejora nueva Core Vitality necesitaba algún valor: le puse los de la propuesta como **provisorios**.
7. **MD 09 A, tutorial deshabilitado — resuelto:** el componente `TutorialManager` (`Managers/Tutorial`) estaba desactivado desde el commit `8148e40` (24/10). Lo reactivé en `2805fad`. Probado en Play Mode: en una partida normal (tutorial ya completado) queda inactivo y todo está permitido; con el botón Tutorial los 14 pasos avanzan con sus eventos, y Skip libera todo.
8. **MD 09 A, textos del tutorial:** el MD pedía revisarlos antes del commit. Como no estabas, los commiteé; están abajo para que los revises y te los cambio.
9. **MD 09 B, "+N cells":** `TileOptionButton` muestra el camino simple más largo entre la entrada y una salida del layout (DFS sobre las celdas Path, barato). **Con los 25 layouts actuales siempre da +5**, porque todos van del centro de un borde al centro de otro. El cálculo exacto con `IRunnerService.TryGetRouteLength` exige simular la colocación y no aporta más. Opciones: (a) dejarlo así; (b) mostrar la cantidad de celdas Path (varía de 5 a 9); (c) sacarlo.
10. **MD 09 E, disolve de la niebla:** el shader de la niebla es `Assets/Shader/Clouds.shadergraph` y **no tiene `_Dissolve`**. `FogOfWarSystem` usa `_Dissolve` (y `_DissolveDirection`) por `MaterialPropertyBlock` si el material lo tiene. Si no lo tiene, hace una animación equivalente: el fog tile se encoge desde el lado más cercano al tile colocado y se hunde, con retardo por distancia y ruido, en 0.4 s. Recién después vuelve al pool. Para el disolve real ver el checklist.
11. **MD 09 C, retroceso de cañón:** solo la Tesla tiene un hijo reconocible (`Cañon`). Basic, Fire y Sniper son modelos con huesos sin un "barrel" claro: asigná `TurretShotFeedback.barrel` en cada prefab si querés retroceso. El muzzle flash funciona en todas.
12. **MD 08 D, conflicto menor:** el MD ubica la colocación de torretas en `ShiftingWorldUI`, pero en la escena `placeWithUI` está desactivado y la hace `TurretPlacer`. El fantasma (`TurretGhostPreview`) funciona por los dos caminos.

---

## MD 01 — Esencias por kill (`8a31777`)

Hecho: `essenceReward` en `EnemyData` y en los 15 assets; `IsSummoned`/`MarkAsSummoned`; `WorldSwitchPoints` escucha kills y cambios de modo; `ResetTotals()`; popup agrupado (`batchWindow` 0.5 s, tiempo unscaled); `blue/redEssenceMultiplier`.

| Criterio | Verificado | Cómo |
|---|---|---|
| Cambiar de mundo con J no da esencias | ✅ | Por código: se borró el `Update` que sumaba. |
| Minion en Normal da azul, en Other da roja | ✅ | Prueba en modo Edit (en tu equipo la mejora roja está en nivel 5, ×1.5). |
| Llegar al core no da esencias | ✅ | Prueba en modo Edit. |
| Los minions invocados dan 0 | ✅ | Prueba en modo Edit con `MarkAsSummoned`. |
| Black Knight da esencias una sola vez | ✅ | Por código: `OnPreDeath` + guard `_removedFromWave`. |
| ResultScene y EssenceBank igual que antes | ✅ | Por código: `EndRun` no cambió. |
| Reiniciar resetea a 0 | ✅ | Prueba en modo Edit con `ResetTotals()`. |

## MD 02 — Sin tope de nivel (`d19587a`)

Hecho:
- Se borraron `maxUpgradeLevel` y `MaxUpgradeLevel`.
- Se borraron los 4 scripts `TileLevelCap*` y el asset `TileLevelCapConfig`. El componente estaba en `Menu.unity` (GameObject `UpgradeSystem`) y lo saqué con el Editor.
- `Menu.unity` se reserializó con el formato de Unity 6.3, por eso el diff es grande; no hay cambios de contenido.

| Criterio | Verificado | Cómo |
|---|---|---|
| Torreta pasa de nivel 15 | ✅ | Prueba en modo Edit: nivel 20 con daño 52.4 y multiplicador 4.8. |
| No quedan referencias a `TileLevelCap*` / `MaxUpgradeLevel` | ✅ | grep en scripts, escenas y prefabs. |
| Sin "Missing Script" | ✅ | Revisión de las 4 escenas y todos los prefabs. |

## MD 03 — Duplicados de tiles (`4da6a65`)

Hecho:
- `Common/DupeProgression.cs` (requisito inicial 2, ×1.5 con `CeilToInt`), compartido por torretas y tiles.
- El sistema de tiles usa `TileLayout` como clave. Usé **la referencia del asset**, no `layout.name`, que es la opción que el MD permite cuando puede haber nombres repetidos.
- Sin bloqueo por mejora: se borraron `TileLevelingPermissionProvider` (estaba en `Menu.unity`, lo saqué), `TileStats`, `TileDataSO` y los 25 assets `TileData`.
- `GridGenerator`: se limpiaron los campos viejos en la escena y en `Managers.prefab`. El tile inicial no suma duplicado.
- Multiplicadores de nivel aplicados a todos los `PathCellEffect` del mismo layout.
- `slowMax` y `stunMax` aplicados.
- UI: se sacó `DontDestroyOnLoad` de `TileDupeUI` y `TurretDupeUI`. La etiqueta de cada opción muestra `"{layout}  Lv N (x/y)"` y la barra se actualiza por layout.

| Criterio | Verificado | Cómo |
|---|---|---|
| 2 copias suben a nivel 2, mismos requisitos que torretas | ✅ | Prueba en modo Edit: tras 40 copias, torreta y tile quedan en `Lv 6 (10/18)`. |
| DPS y Slow no son duplicados entre sí, ni dos formas distintas | ✅ | Prueba en modo Edit. |
| Al subir de nivel, todas las copias DPS hacen más daño | ✅ | Prueba en modo Edit: las dos copias pasan de 10 a 12 de daño. |
| Slow ≤ `slowMax`, stun ≤ `stunMax` | ✅ | Prueba en modo Edit: slow 2 → 0.95, stun 50 → 10. |
| Reiniciar vuelve todo a nivel 1 | ✅ | Por código: sistemas y UI se recrean con la escena y `GenerateFirst` llama a `ResetAllLevels`. |
| No hace falta comprar ninguna mejora | ✅ | El bloqueo está borrado. |

**Nota:** desde el MD 08, los duplicados de torretas también se cuentan al **colocar** (antes se contaban al elegir la opción, aunque después canceles).

## MD 04 — Modificadores (`e098157`)

Hecho:
- `ModifierStat`, `ModifierSO` y el `GameModifiersManager` nuevo (stats sumados; propiedades calculadas; pools; `GetAvailable`; `OnModifierApplied`; recálculo de torretas; `Core.ModifyMaxHealth`).
- `ModifierWaveTrigger` (cada 3 oleadas, nunca después de la 45, pausa con `GameTime`). `NextWaveCountdownRoutine` usa `Time.deltaTime`, así que se frena con la pausa.
- `ModifierCardUI`, `ModifierPanelSelection.OpenPanel(options, cb)` / `OpenExtraPick()`, `ActiveModifiersHUD` + `ModifierIconHover`.
- `TurretDamageCalculator` aplicado en Projectile, BasicRaycastShooter, FireTurret y TeslaTurret.
- `TurretType.Sniper` y un campo nuevo `turretType` en `TurretDataSO`, asignado en las 5 torretas. No existía ningún campo de ese tipo: `type` es un string.
- Menú **Tools/Shadow Realms/Create Modifier Assets**: ya lo ejecuté y creó los 30 assets en `Assets/Scriptable Object/Modifiers/Normal|Other` (la carpeta real tiene espacio).
- En GameScene (`Managers/Game Modifier`): pools asignados (15 Normal, 15 Other) y `ModifierWaveTrigger` agregado.

Integraciones y detalles por modificador:

| Modificador | Integración | Nota |
|---|---|---|
| N01, O01, N14 | Velocidad de la barra en `ShiftingWorldMechanic` | |
| N02, O02, N14 | Cantidad de opciones, limitada por la cantidad de botones | |
| N03 | Reroll | Necesita un botón en el Editor |
| N04, N05, N06 | `PathEffectManager` | |
| N07 | Celdas sin efecto hacen daño | |
| N08 | El slow y el stun duran X s más | Se suma a la duración de cada aplicación |
| N09, O10 | Duplicado extra | |
| N10 | Velocidad de los enemigos | |
| N13, O13, O15 | Esencias | |
| N15 | Carga la barra de tiles al cambiar a Normal | |
| O06 | Wildfire | Radio de contagio `spreadRadius` 2.5, configurable |
| O07 | Tesla | +1 objetivo |
| O08 | Deadeye | Cada 4.º disparo de la Sniper hace daño x2 |
| O11, O12 | Ejecutar / Boss Hunter | Solo daño de torretas |
| O14 | Blood Price | |
| O15 | Horde Bounty | Cuenta y spawn reales, vía `WaveManager.EnemyCountMultiplier` |

| Criterio | Verificado | Cómo |
|---|---|---|
| Al terminar la oleada 3 se pausa y aparecen 3 cards | 🟡 | Lógica verificada. **Necesita el panel en el Editor** (checklist). |
| En modo Other solo salen modificadores O | ✅ | Prueba en modo Edit con `GetRandomAvailable`. |
| Un modificador elegido no vuelve a salir | ✅ | Prueba en modo Edit. |
| O03 + O14 = +55% de daño | ✅ | `turretDamageMultiplier` = 1.55 en modo Edit. |
| N10 hace más lentos a los enemigos | ✅ | ×0.92 en modo Edit; se aplica en `Enemy.Update`. |
| O15 da más enemigos reales y ×2 esencias | ✅ | ×1.3 y ×2 en modo Edit; afecta a `SpawnManager` y a `WaveManager`. |
| Después de la oleada 45 no aparece el panel | ✅ | Por código: `IsLastWave()`. |
| Íconos en el HUD | 🟡 | Código listo. **Necesita el HUD en el Editor.** |
| Al reiniciar no queda ningún modificador | ✅ | Prueba en modo Edit con `ResetRun`; el manager se recrea con la escena. |

## MD 05 — Niebla y zoom (`5e8d039`)

Hecho:
- `GridGenerator.OnTilePlaced`, `TileFootprint` (= 5, los 25 layouts son 5×5 de tamaño 1), `TileGap`, `InitialLayout`, `PlacedTileCount` y `GetPlacedTile`.
- `FogOfWarSystem`: grilla con pool, expansión, revelado por superposición, cambio de material con `sharedMaterial`, `OnFogRevealed` y la esencia de N11.
- Layer **Fog** creada (índice 10).
- GameObject `FogOfWar` en GameScene con los materiales `M_Blue` / `M_Red` del plano viejo.
- Sin prefab de niebla asignado, usa un quad generado en runtime.
- `CameraController`:
  - zoom suave con tiempo unscaled;
  - ignora el scroll sobre la UI;
  - expone `ZoomNormalized` y `OnZoomChanged`;
  - mantiene Q/R;
  - escribe el FOV en el `CinemachineCamera` del mismo objeto y en la `Camera`.
- Los spawn points son celdas de exit de tiles ya colocados, así que la niebla nunca los tapa.

| Criterio | Verificado | Cómo |
|---|---|---|
| Niebla alrededor del tile inicial | ✅ | Prueba en modo Edit: 288 fog tiles (17×17 menos el inicial). |
| Colocar un tile quita la niebla que tapa | ✅ | Prueba en modo Edit: revela 1, y 2 con superposición parcial; la grilla se expande. |
| La niebla no intercepta clicks | ✅ | Por código: los colliders se desactivan y se destruyen, y la colocación usa la layer `Cell`. |
| La niebla cambia de material con el mundo | ✅ | Por código (`WorldState.OnWorldChanged`). Falta verlo en Play Mode. |
| Zoom suave; no hace zoom sobre la UI | 🟡 | Por código. Falta probarlo en Play Mode. |
| N11 suma esencia azul al revelar | ✅ | Por código, vía `AddBlueEssence`. El HUD se sincroniza con `SetTotals`. |
| FPS | ❌ | No medí con el Profiler. Son unos 300 quads con material compartido. |

## MD 06 — Zonas de recompensa (`f8bb878`)

Hecho:
- `RewardType`, `RewardTableSO` (asset en `Assets/Scriptable Object/Rewards/RewardTable.asset` con los pesos del MD), `RewardZone` (LineRenderer en loop con pulso y un ícono opcional) y `RewardZoneSystem`.
- Spawn, visibilidad por zoom o N12, reclamo, entrega con fallback, toast y VFX/SFX opcionales.
- GameObject `RewardZones` en GameScene con sus refs.
- Sin prefab, crea la zona en runtime con material `Sprites/Default`.

| Criterio | Verificado | Cómo |
|---|---|---|
| 3 zonas al inicio; ocultas con zoom in | ✅ | Prueba en modo Edit: 3 zonas entre 2 y 6 tiles, ocultas. |
| Llegar con el camino da la recompensa y la zona desaparece | ✅ | Prueba en modo Edit. |
| Ninguna zona aparece sobre un tile colocado | ✅ | Solo usa celdas con niebla y descarta las que se superponen. |
| `GetRandomCandidateSet` no cambia | ✅ | No se tocó. |
| N12: siempre visibles y +50% | ✅ | Prueba en modo Edit: 25 → 38. |
| Cada 5 oleadas aparece una zona nueva si hay menos de 3 | ✅ | Por código (`OnWaveEnded`). |

## MD 07 — Mejoras permanentes (`de36b87`)

Hecho:
- `UpgradeLvlCapTile.asset` pasó a **Swift Builder** (`upgrade_tileBarSpeed`, 3 niveles, +10% por nivel, se suma a N01).
- `UpgradeTileDupeSystem.asset` pasó a **Treasure Seeker** (`upgrade_rewardZoneBonus`, 3 niveles, +20% por nivel, se suma a N12).
- More Essences pasó a "per kill"; los IDs no cambiaron.
- Mejora nueva **Core Vitality** (`upgrade_coreHealth`, categoría nueva `General`, 5 niveles, +2 de vida por nivel, la lee `Core.Start`).
- Compra con las dos monedas permitida para `General` (la validación existente ya solo restringe Lab y Workshop). Agregada al `UpgradeRepository`.
- Los niveles que hubieras comprado con los IDs viejos (`UpgradeLvlCapTile`, `UpgradeTileDupeSystem`) no se migran.

| Criterio | Verificado | Cómo |
|---|---|---|
| Laboratory sin Lvl Cap ni Tile Dupe System | ✅ | El repositorio lista More Normal Essences, Swift Builder y Treasure Seeker. |
| More Essences dice "per kill" | ✅ | Assets actualizados. |
| Tabla de ingresos y costos | ✅ | Abajo. |
| Core Vitality sube la vida inicial | 🟡 | Por código. **Necesita una lista General en el menú** (checklist). |

### Ingresos esperados por partida

Base sin mejoras ni O15. Se calculó con `WaveList` × `essenceReward`, sin minions invocados y sin zonas de recompensa (que suman unos 25 por zona). El reparto azul/rojo depende del tiempo en cada modo; la tabla supone 50/50.

| Muerte en | Esencias totales | Azul (≈50%) | Rojo (≈50%) |
|---|---|---|---|
| Oleada 15 (14 completas) | 316 | ~158 | ~158 |
| Oleada 30 (29 completas) | 1582 | ~791 | ~791 |
| Oleada 45 (44 completas) | 4348 | ~2174 | ~2174 |
| Victoria (45 completas) | 4698 | ~2349 | ~2349 |

Los costos actuales son planos y muy bajos para estos ingresos: Lvl Cap 15, More Essences 5, Tile Dupe 10, mejoras de torreta 15. Con una partida a la oleada 15 se compraba casi todo.

### Costos propuestos (por nivel, **sin aplicar**)

| Mejora | Moneda | Nv1 | Nv2 | Nv3 | Nv4 | Nv5 | Total |
|---|---|---|---|---|---|---|---|
| Swift Builder | Azul | 60 | 120 | 200 | – | – | 380 |
| Treasure Seeker | Azul | 50 | 100 | 170 | – | – | 320 |
| More Normal Essences | Azul | 30 | 60 | 100 | 150 | 210 | 550 |
| Turret Damage | Rojo | 40 | 70 | 110 | 160 | 220 | 600 |
| Turret Fire Rate | Rojo | 40 | 70 | 110 | 160 | 220 | 600 |
| Turret Range | Rojo | 40 | 70 | 110 | 160 | 220 | 600 |
| More Other World Essences | Rojo | 30 | 60 | 100 | 150 | 210 | 550 |
| Core Vitality (provisorio, ya aplicado) | Azul + Rojo | 40+40 | 80+80 | 130+130 | 190+190 | 260+260 | 700+700 |

Totales del árbol: unas **1950 azules** y **3050 rojas**. Eso equivale a unas 2 o 3 partidas que lleguen a la oleada 30, o a unas 12 que mueran en la 15, para completarlo.

## MD 08 — Previews en selección y colocación (`6bea087`)

Hecho:
- **Íconos de tiles:** `Tiles/UI/TileIconGenerator` (textura de 16 px por celda, `FilterMode.Point`, cacheada por layout) con los colores de `TileIconPalette` (asset en `Assets/Scriptable Object/Tiles/`). Borde blanco en la entrada, punto blanco en las salidas.
- **Botones:** `Tiles/UI/TileOptionButton` y `Turret/UI/TurretOptionButton` (preview, nombre chico, "Lv X", barra de copias, badge "LEVEL UP!" y tooltip). El tooltip lo dibuja `UI/OptionTooltip`, que se crea solo si no existe. `ShiftingWorldUI` usa `Bind` si el botón tiene el componente y si no, el texto de antes.
- **Íconos de torretas:** campo `icon` en `TurretDataSO` y menú **Tools/Shadow Realms/Bake Turret Icons** (y "(Force)"). Ya lo corrí: 5 PNG en `Assets/Sprites/TurretIcons/` asignados a los 5 assets.
- **Fantasma de tile:** `GridGenerator.TryGetPlacementForExit` reutiliza `TryFindPlacement` (nada duplicado) y devuelve `TilePlacementInfo`. `GhostLayoutInstantiator` usa los mismos prefabs de celda, pero sin colliders ni componentes de gameplay. `TileGhostPreview`:
  - cachea por layout, rotación y flip;
  - entra con escala 0.9 → 1 y alpha 0 → 0.6 en 0.12 s unscaled;
  - ilumina la zona de recompensa con "Reward!" y su ícono;
  - con N11 muestra "+N".
- **Shader y layer:** `Shader/Ghost/GhostFresnel.shader` (URP unlit transparente con fresnel), material `Assets/Materials/Ghost/M_Ghost.mat` y layer **Ghost** (11), que `cellLayers` excluye.
- **Botones de salida:**
  - flecha rotada según la dirección de salida, más el mini ícono del tile;
  - hover muestra el fantasma;
  - Tab y Shift+Tab recorren las salidas y Enter confirma;
  - click derecho o Escape cancela, como antes.
- **Fantasma de torreta:** `Turret/Ghost/TurretGhostPreview` sigue al mouse por las celdas, verde en las libres y rojo en las ocupadas, con el círculo de rango del nivel actual. `CellSlot.GetPlacementPose` sale de `TryPlace`.
- **Arreglos:**
  - el duplicado de torreta se suma recién al colocar;
  - cancelar vuelve a abrir el panel (antes, con `TurretPlacer`, la elección quedaba trabada);
  - el panel se cierra en el acto, sin las esperas de 1.5 s.

| Criterio | Verificado | Cómo |
|---|---|---|
| 3 imágenes de layouts con colores, nombre y nivel chicos | ✅ | Play Mode + captura |
| Íconos de torretas | ✅ | Play Mode + captura (5 íconos generados) |
| Fantasma exacto; el tile real coincide | ✅ | Play Mode: 25 de 25 celdas con la misma posición y rotación |
| El fantasma no se clickea ni afecta enemigos | ✅ | 0 colliders, 0 `CellSlot`, 0 `PathCellEffect`, layer Ghost |
| Fantasma de torreta verde/rojo con rango | 🟡 | Pose idéntica a la real (error 0). El tinte por hover no se probó con el mouse real |
| Cancelar no suma duplicado | ✅ | Play Mode |
| El panel de torretas se cierra en el acto | ✅ | Play Mode |

## MD 09 — Game feel y pulido

Todo lo nuevo está en `Assets/Scripts/Feedback/`. Los valores globales (shake, cámara lenta, flash, orbes, chime y hits) están en `Assets/Resources/FeedbackSettings.asset` (clase `FeedbackSettings`, que se carga sola). El feedback de UI usa tiempo unscaled; el del mundo, tiempo escalado.

### A. Riesgos técnicos (`7a8a9c9`)
- **`ModalPanelQueue`** (en `Managers/ModalPanelQueue`): `Enqueue(open, isOpen, prioridad)`. Abre de a un panel, con prioridad modificador > extra pick > tiles/torretas. Tiles, torretas, modificadores y extra pick pasan todos por la cola. Mientras hay un modificador abierto, las barras de modo esperan en 100.
- **`Core/Log.cs`**: `Log.Info` / `Log.Warn`, con `[Conditional("UNITY_EDITOR")]` y formato `[Clase] mensaje`. Reemplazó 34 `Debug.Log` de código frecuente (dupes, barras, cambio de mundo, colocación, oleadas, niebla, zonas). Se borraron 4 logs basura de `TurretDupeUI` que no tenían prefijo.
- **Tutorial:**
  - eventos nuevos `RaiseModifierChosen`, `RaiseRewardClaimed` y `RaiseZoomedOut` (este último cuando el zoom pasa 0.8);
  - 5 pasos nuevos;
  - el paso viejo "Essences Per World Switch" quedó desactualizado por el MD 01 y lo reemplacé por "Essences Per Kill".

| Criterio | Verificado | Cómo |
|---|---|---|
| Nunca dos paneles a la vez | ✅ | Play Mode: con las dos barras en 100 se abre solo tiles; torretas espera y se abre al colocar el tile. Orden por prioridad comprobado |
| Un build de desarrollo no muestra logs frecuentes | ✅ | Por código: `Conditional("UNITY_EDITOR")` |
| El tutorial cubre las mecánicas nuevas | ✅ | Play Mode: los 14 pasos avanzan en orden (`TutorialManager` reactivado en `2805fad`) |

**Textos del tutorial para revisar** (en `TutorialManager.BuildSteps`):

| Paso | Título | Texto | Avanza con |
|---|---|---|---|
| 3 | Fog Of War | The land around your path is hidden by fog. Every tile you place clears the fog under it. Zoom out with the MOUSE WHEEL (or R) to see more of the map. | Zoom out |
| 4 | Place Your First Tile | Choose a tile and place a path. Hover an exit arrow to preview where the tile will go. | Tile colocado |
| 5 | Tile Copies | Every time you place a tile you already own, it gets a copy. Collect enough copies and that tile levels up: all of its cells deal more damage, slow more or stun longer. The bar under each option shows its copies. | Continue |
| 6 | Reward Zones | Golden squares inside the fog are reward zones. Place a tile over one to claim it: essences or an extra modifier. | Continue |
| 12 | Essences Per Kill | Every enemy you kill gives essences. In the Normal World you earn blue essences, in the Other World red essences. They are your XP: blue fills the Normal bar (tiles), red fills the Other World bar (turrets). | Continue |
| 13 | Modifiers | Every few waves you will choose one modifier out of several cards. Modifiers last the whole run. The cards depend on the world you are in. | Continue |

Los pasos de modificador y de zona son "Continue", porque el tutorial termina después de la primera oleada y no llega a ofrecer ninguno. Los eventos `ModifierChosen` y `RewardClaimed` quedan disponibles por si se reordena.

### B. Economía y decisiones (`b94514f`)
- **`EssenceOrbFX`** (en el Canvas del HUD):
  - cada kill que da esencias manda un orbe con pool por una bezier con control arriba, en 0.5–0.7 s con ease-in;
  - es una gota azul en Normal o una llama roja en Other, para no depender del color;
  - máximo 20 orbes; los que sobran se suman al último;
  - al llegar, el contador hace un punch de 1.15 en 0.1 s y recién ahí suma. Para que el HUD no salte, `EssencePopupUI` lleva la cuenta de lo que está "en vuelo".
  - Las ganancias externas (zonas, N11) siguen usando el popup de texto.
- **`EssenceChime`**: pitch +0.05 por kill dentro de 1 s, con tope de +0.5. No había SFX de esencia: genera un chime en runtime, enrutado al grupo `SFX` del mixer. Se puede asignar un clip.
- **"+N cells"** en `TileOptionButton` (ver decisión 9).
- **`UIController`**: texto "Modifier in N waves", que pulsa cuando falta 1. Lo calcula `ModifierWaveTrigger.WavesUntilNextOffer()`. Se oculta si no hay panel de modificadores, si no quedan modificadores o después de la última oferta.

| Criterio | Verificado | Cómo |
|---|---|---|
| Cada kill manda un orbe del color/ícono y el número sube al llegar | ✅ | Play Mode + captura; real 4, mostrado 0 hasta que llegan |
| Aviso de modificador en el HUD | ✅ | Play Mode ("Modifier in 3 waves" y pulso con 1). Necesita el panel de modificadores para aparecer en el juego real |

### C. Combate (`3631ad7`)
- **`HitFlash`**, en los 15 prefabs de enemigos:
  - emisión blanca de 0.06 s por `MaterialPropertyBlock` y punch de 1.08 en 0.08 s;
  - como máximo un flash cada 0.05 s;
  - activa `_EMISSION` en instancias del material; para que esa variante entre en el build, `FeedbackSettings` referencia `M_LitEmissionVariant`.
- **`EnemyDeathFeedback`**: VFX de muerte por zona con `VfxPool`. Los prefabs nuevos están en `Prefabs/Effects/Death/` (goblin verde, esqueleto hueso, vampiro carmesí), más grandes para Big, MiniBoss y Boss. **No hay shader de disolve en el proyecto**, así que el enemigo vuelve al pool como antes.
- **`TurretShotFeedback`**:
  - retroceso del cañón y muzzle flash con pool en `BasicProjectileShooter`, `BasicRaycastShooter`, `FireTurret` y `TeslaTurret`;
  - trazado de 0.05 s en la Sniper;
  - `LineRenderer` con ruido entre los saltos de la Tesla.
- **`DamageNumbers`**: textos con pool y máximo 40. Los golpes de más del 30% de la vida máxima salen más grandes y en naranja. `DamageTextPopup` ahora sirve para texto 3D y de UI.
- **`BossFeedback`**:
  - banner "BOSS: nombre" de 2 s;
  - pan de cámara suave al spawn y vuelta;
  - barra de vida grande arriba;
  - al morir, cámara lenta de 0.3 s a 0.3x (`GameTime.RequestSlowMotion`, nuevo) y shake fuerte.

| Criterio | Verificado | Cómo |
|---|---|---|
| Cada golpe se nota (flash y punch) | ✅ | Play Mode + captura |
| Las muertes tienen VFX | ✅ | Play Mode (`VFX_Death_Skeleton` en el pool) |
| Los bosses tienen banner y barra | ✅ | Play Mode + captura; la barra baja a 0.60 tras el 40% de daño |
| Cámara lenta y shake al morir el boss | ✅ | timeScale 0.3 y trauma 0.9; se libera a 1 |

Bugs encontrados en la prueba y corregidos:
- el primer golpe tras reaparecer del pool no generaba número (`EnemyHealth.OnHealthReset`, nuevo);
- el VFX de muerte no salía porque el enemigo vuelve al pool antes de `OnDied`.

### D. Core y cámara (`ef4a2b6`)
- **`CameraShake`** en Main Camera:
  - shake por trauma (`AddTrauma`) que decae, con offset Perlin;
  - se aplica solo durante el render (`beginCameraRendering` / `endCameraRendering`), así no pelea con `CameraController` ni con el pan;
  - intensidad desde `FeedbackSettings` y la PlayerPref `Options_CameraShake` (lista para el bloque F).
- **`CoreHitFeedback`**:
  - viñeta roja de 0.3 s como overlay de UI, porque la escena no tiene un Volume con viñeta para el core;
  - shake corto y pulso del texto de vida en `UIController`;
  - con 3 de vida o menos, la viñeta queda tenue y fija.

| Criterio | Verificado | Cómo |
|---|---|---|
| Un golpe al core tiene viñeta y shake | ✅ | Play Mode + captura; con vida 3 la viñeta queda en 0.28 |

### E. Features nuevas (`7bbdbdb`)
- **Niebla:** disolve en 0.4 s con dirección y ruido (decisión 10).
- **Zonas de recompensa:**
  - el borde ya pulsaba;
  - al reclamar: haz vertical de 0.6 s, burst de partículas del color de la recompensa (`Prefabs/Effects/Feedback/VFX_RewardBurst`), toast con lo ganado (se crea en el HUD si no está asignado) y un SFX por `RewardType` (clip por tipo o tono generado con pitch distinto).
- **`LevelUpFeedback`**: con `OnTurretLevelUp` / `OnTileLevelUp`, todas las instancias del tipo o layout hacen flash a la vez, aparece un "Lv X" flotante sobre cada una y suena un SFX con pitch que sube con el nivel.
- **Cambio de mundo:**
  - cámara lenta de 0.15 s a 0.3x;
  - aberración cromática y distorsión de 0.2 s (`WorldShiftPostFX`, Volume global creado en runtime);
  - crossfade de 0.5 s con barrido de low-pass en `ShiftingWorldAudio`.
  - Si apretás J durante el cooldown, el anillo hace shake y suena un buzz de "no disponible".
- **Cards de modificadores:** entrada escalonada (0.08 s, escala 0.8 → 1 y alpha), hover a 1.05 con brillo del color del modo, y al confirmar la elegida crece mientras las otras se desvanecen. Todo unscaled.
- **Colocación de tiles:** **no había ningún `TilePlacementSequencer` en la escena**, así que las celdas no se animaban. Lo agregué al `GridManager`. Ahora cada celda cae con rebote y polvo (`VFX_TileDust`), escalonada por distancia a la entrada.

| Criterio | Verificado | Cómo |
|---|---|---|
| La niebla se disuelve | ✅ | Play Mode (variante de transformación) |
| Las zonas tienen haz al reclamarse | ✅ | Play Mode + captura (haz, burst, toast) |
| Level up con flash en todas las instancias | ✅ | Play Mode: 52 renderers, etiqueta y pitch 1.08 |
| Cambio de mundo con cámara lenta y crossfade | ✅ | Play Mode: timeScale 0.3 y vuelta a 1; peso del volume 0; low-pass vuelve a 22 kHz |
| Cards con entrada, hover y confirmación | ✅ | Play Mode con un panel temporal |
| Las celdas caen con rebote y polvo | ✅ | Play Mode + captura (25 celdas animando) |

### FPS (Profiler)
Oleada más densa (**41, 200 enemigos**), velocidad **x3**, 12 torretas, vida infinita, todo el feedback activo. Muestras del `ProfilerRecorder` "Main Thread", 4000 frames en el **Editor**:

| Promedio | p50 | p95 | p99 | Frames > 16.7 ms | Frames > 33 ms |
|---|---|---|---|---|---|
| 7.3 ms (~136 FPS) | 5.6 ms | 12.7 ms | 15.5 ms | 17 | 4 |

Los 4 picos coinciden con capturas de pantalla y comandos del MCP que mandé durante la medición. Es una medición en el Editor; conviene repetirla en un build de desarrollo.

## MD 10 — Torretas nuevas (`cd725a7`)

El juego pasa de 5 a 13 torretas. Los assets están en `Assets/Scriptable Object/Turrets/Turrets/` y los prefabs en `Assets/Prefabs/Turrets/`; todas quedaron en `Database.asset` y con ícono. Se generan con **Tools/Shadow Realms/Create New Turrets (MD 10)** (`Editor/NewTurretsCreator.cs`), que se puede volver a correr.

**Modelos:** no hay modelos de catapulta, balista ni estandarte en el proyecto (`PolygonDungeonRealms` solo trae 5 prefabs de escenario). Cada torreta reutiliza uno de los 5 modelos existentes, con materiales tintados propios (`Assets/Materials/Turrets/`) y alguna pieza extra: el estandarte del War Banner, la hoja de la Guillotine y la cúpula del Warden. Son provisorios.

| Torreta | Base | Implementación |
|---|---|---|
| Mortar | Fire | `MortarTurret` + `MortarProjectile` (parábola al punto del suelo, daño en área con `TurretDamageCalculator`, círculo de impacto durante el vuelo) + `MinRangeTargeting` (2.5) |
| Ballista | Sniper | `BallistaTurret`: perforación en línea con −10% por enemigo previo (mínimo 50%). Targeting `PriorityTargeting.MostAdvanced` (enemigo más adelantado en la ruta) |
| Rift Cannon | Tesla | `BasicRaycastShooter` + `RiftCannonTurret` (`IDamageMultiplierProvider`): +8%/s en el mismo mundo, tope +200%, descarga al cambiar de mundo, emisivo según la carga |
| War Banner | Basic | `WarBannerAura`: cada 0.5 s busca torretas en 2.5 celdas y registra `TurretStats.AddExternalBuff`. Varios banners **no se suman** (se toma el mayor). +3% de daño y +2% de cadencia por nivel |
| Purifier | Tesla | `PurifierTurret`: rayo continuo, tick cada 0.2 s (`fireRate` 5, `damage` = DPS). x3 contra escudo de vida; contra la invulnerabilidad de `LinkedShield` hace el 100% con `DamageFlags.IgnoreInvulnerable`. Targeting `Shielded` |
| Guillotine | Sniper | `GuillotineTurret`: `damage` del asset = % de vida máxima (8), 4% contra bosses, +0.5% por nivel. Targeting `HighestMaxHealth` |
| Reaper | Basic | `ReaperTurret`: marca `Enemy.LastHitSource` solo si su golpe mata; `WorldSwitchPoints` multiplica por +100% (+10% por nivel). Guadaña breve en cada kill |
| Warden | Energy | `WardenAura` + `WardenRegistry`: protege en 2.5 celdas (+0.25 por nivel). `Ability_DisableTowersEMP` y `Ability_DestroyRandomTurretOnLowHP` lo consultan y muestran un VFX de "bloqueado" |

Otros cambios:
- `TurretType` tiene los 8 tipos nuevos. Las de soporte usan `type = support`, así `Turret` no exige shooter.
- `EnemyHealth`: `TakeDamage(float, DamageFlags)`, sin romper la firma vieja. `HasShield` = invulnerable o escudo de vida (`ShieldExtra`, que se descuenta primero al recibir daño).
- `TurretTargeting`: hooks `IsValidTarget` / `PriorityScore` para las subclases.
- `TurretInfoPanel`: texto `ExtraInfo` arriba del panel con buffs del banner, protección del Warden, carga del Rift y los datos propios de cada torreta (`ITurretInfoProvider`).
- `TurretDupeUI.GetTurretData`: si una torreta todavía no tiene copias, devuelve nivel 1 con 0/2 en lugar de 0/0.

**Diferencia con el MD:** el MD trata a `ShieldOnLowHP` como una invulnerabilidad, pero en el código ese escudo es un boost de vida (`TryApplyShieldAuraBoost`), igual que `ShieldAura`. El Purifier lo trata como escudo de vida (x3). La única invulnerabilidad real es la de `LinkedShield`.

| Criterio | Verificado | Cómo |
|---|---|---|
| Las 8 se eligen en el panel, se colocan y suben de nivel | ✅ | Play Mode: aparecen en el panel; las 8 colocadas y en nivel 2 tras 2 duplicados |
| Mortar no dispara a enemigos pegados y hace área | ✅ | Play Mode: sin objetivo a 1.2 u; la explosión daña al del centro y al vecino a 1 u, no al de 4 u |
| Ballista daña a toda la fila | ✅ | Play Mode: 3 enemigos en línea reciben 100/90/80%; el de afuera, 0 |
| Rift Cannon crece y se resetea | ✅ | Play Mode: +200% tras 60 s, 0% al cambiar de mundo, +3% medio segundo después |
| War Banner: buff visible y no se suma | ✅ | Play Mode: +18%/+12% en nivel 2, visible en el panel; con 2 banners el multiplicador queda igual |
| Purifier daña al Nigromante con `LinkedShield`; el resto no | ✅ | Play Mode: un golpe normal de 500 hace 0; 5 ticks del Purifier hacen 12.7 |
| Guillotine saca un % visible a un boss | ✅ | Play Mode: 4.5% al Boss Goblin (45 de 1000), 8.5% a un Heavy (nivel 2) |
| Reaper da el doble de esencias | ✅ | Play Mode: kill normal 2, kill del Reaper 4 |
| Warden: el Ángel Caído no desactiva ni destruye dentro del radio | ✅ | Play Mode: la torreta protegida no se desactivó ni se destruyó en 3 intentos; las demás sí |
| Las 13 torretas tienen ícono | ✅ | `Bake Turret Icons`: 13 sprites |

---

## MD 11 — Variantes de tiles (`7262457`)

- 10 layouts nuevos en `Scriptable Object/Tiles/Special` (ForkT, DeadEnd, Serpent, Plaza, Hybrid-SlowDps, Hybrid-StunDps, UpDPS-Normal, UpSlow-Other, UpExposure, LeftAntiHeal) y prefabs `PathExposure` / `PathAntiHeal`. Menú `Tools/Shadow Realms/Create Tile Variants (MD 11)`.
- Espejos: `TileLayout.mirrorOf` / `DupeKey`. Comparten duplicados y se desbloquean con su original.
- Dead End: nunca cierra el último carril; da 15 esencias con orbe.
- Bloqueados por mundo: inactivos y desaturados en el otro mundo, ×2 en el suyo.
- Exposure: el enemigo pasa a ser targeteable. Anti-heal: bloquea `Heal`.

**Pares espejados**: Right → Left, RightDPS → LeftDPS, RightSlow → LeftSlow, RightStun → LeftStun, Up-Right → Up-Left, Up-RightDPS → Up-LeftDPS, Up-RightSlow → Up-LeftSlow, Up-RightStun → Up-LeftStun.

## MD 12 — Sinergias, élites, bounty y legendarios

### A. Sinergias (`6ba8c17`)
- `SynergyTableSO` (13 reglas, `Scriptable Object/Synergy/SynergyTable.asset`), `TurretSynergy`, `SynergyService`.
- Probado: Tesla junto a un tile Stun muestra "+30% damage vs stunned" en el fantasma y en el panel; daño 10 → 13 contra aturdido; colocar el tile Stun después activa la sinergia de la Tesla ya puesta.

### B. Élites (`5caa7d3`)
- `EliteSpawnRules` (0% / 6% / 12% por zona), afijos Hasted, Armored, Regenerating, Shielded y Splitter. Aura de color y etiqueta con el prefijo.
- Probado: tasas 0,00% / 6,08% / 11,74% en 20.000 tiradas; 0 en bosses. Big2 normal da 6 esencias, élite 18 (×3). Splitter genera 2 Minion2 summoned.

### C. Bounty (`5caa7d3`)
- `BountySpawner` (desde la oleada 4, 15%, no en oleadas de boss), `BountyMarker` (vida ×2, velocidad ×1,4, dorado), `OffscreenIndicator`.
- Probado: toast, flecha en el borde, al morir +30 esencias, al llegar al core no hace daño (10/10 con la vida infinita apagada).
- Nota: el bounty es un Sprint, y los Sprint solo se pueden atacar en su mundo (regla existente).

### D. Legendarios (`083d70c`)
- 6 `ModifierSO` en `Modifiers/Legendary`. `LegendaryTrigger` en las oleadas 15 y 30, con prioridad `Legendary` (3) en `ModalPanelQueue`. Cards con marco dorado y partículas (`LegendaryCardFX`). En el HUD van primero y con marco dorado.
- Probado (con un panel armado en runtime porque la escena no tiene `ModifierPanelSelection`): después de la 15 abre primero el legendario (2 opciones) y luego el común.
  - Unified Ground: dos layouts DPS distintos con 1 copia cada uno quedan en un Lv 2 compartido.
  - Cartographer: revela 24 tiles de niebla, recompensas ×2, zonas siempre visibles.
  - Rift Walker: cooldown 5 s → 1 s.
  - Worldless: multiplicador fuera de su mundo 0,8 → 1,3.
  - Arsenal: colocar una torreta suma 1 copia a los otros tipos colocados, sin recursión. Solo al colocar, no con copias gratis.
  - Primal Ground: implementado, **sin probar en Play Mode**.

## MD 13 — Meta-progresión y loadout (`0465fd2`)

- Eliminado: todo `Upgrades/` y `Scriptable Object/Upgrades/`. Los archivos de tope de nivel ya no existían. Se limpiaron `Core`, `TurretStats`, `TurretOptionButton`, `RewardZoneSystem`, `WorldSwitchPoints` y `ShiftingWorldMechanic`. Menu: se sacaron `UpgradeListUI`, `UpgradeDetailUI`, `UpgradeSystemBootstrap` y `EssenceDisplayUI` de la escena antes de borrar los scripts. Ningún "Missing Script" en escenas ni prefabs (verificado con `GetMonoBehavioursWithMissingScriptCount`).
- Datos: `UnlockTableSO`, `XpCurveSO`, `ProgressionStore`, `LoadoutStore`, `RunLoadout`. `ProgressionSettings` vive en `Resources` (une tabla, curva, tabla de sinergias y tiles de respaldo). Menú `Tools/Shadow Realms/Create Progression Assets (MD 13)` y `Reset Progression`.
- **Todos los assets de la tabla existen** (26 tiles, 13 torretas, 36 modificadores).
- `EssenceBank` quedó como fachada de `ProgressionStore` (lo que menos rompe). Migración: las keys viejas (108 azules / 82 rojas en tu PC) pasaron a XP una sola vez y se borraron → Normal Lv 3, Other Lv 2.
- Menu: Laboratory → **Progression** (dos barras "Lv X — a / b" y la ruta de los próximos 3 niveles con siluetas). Workshop → **Loadout** (modo "Save"). Play abre el Loadout como overlay (Start / Back) antes de cargar la escena.
- Paso de tutorial: la primera vez, el Loadout muestra un texto guía arriba. Se marca como visto al apretar Start.
- Probado en Play Mode:
  - Jugador nuevo: mazo Up / Left / UpDPS + Basic / Fire / Tesla; 5 modificadores por modo.
  - Máximo 5 por columna, las bloqueadas no entran, Start desactivado con menos de 3.
  - GridGenerator solo ofrece tiles del mazo (y sus espejos). Respaldo: con un mazo de solo Dead End y un carril, aparecen Up y Left y se loguea el warning.
  - Torretas: 50 armados de opciones, 0 duplicados y 0 fuera del mazo.
  - Fin de partida +300 / +200: Normal 108 → 408 (Lv 4), Other 82 → 282 (Lv 4). ResultScene anima las barras, 3 level ups con flash y sonido, 5 cards (UpStun, Shock Stones, Mortar, Ballista, Executioner).
  - Lo nuevo aparece en el Loadout y en el pool de modificadores.

### Calibración de la curva

Esencias esperadas por partida (solo kills, `WaveList.asset` + `essenceReward`, 50/50 entre modos → por barra):

| Muere en | Total | Por barra |
| --- | --- | --- |
| Oleada 15 | 421 | ~210 |
| Oleada 30 | 1.834 | ~917 |
| Gana (45) | 4.698 | ~2.349 |

Con los valores del MD (`baseXp` 100, `exponent` 1,4) el nivel 10 cuesta 9.240 XP: unas 12 partidas con el perfil de abajo, y el máximo (29.960 XP) ~24. **Aprobado**: `baseXp` 20, `exponent` 2 en el asset (el código conserva 100 / 1,4 como default).

| Meta | XP | Partidas (perfil mixto*) |
| --- | --- | --- |
| Normal Lv 10 | 5.700 | ~8 |
| Other Lv 14 (máximo) | 16.380 | ~16 |
| Normal Lv 16 (máximo) | 24.800 | ~20 |

*Perfil: 3 partidas muriendo en la 15, 7 en la 30 y después mitad victorias (~1.633 por barra). Las zonas de recompensa, élites, bounty y Dead End suman ~10% más.

## Checklist del Editor

### GameScene — Panel de modificadores (MD 04)
1. Crear un Canvas (o usar el del HUD) → hijo `ModifierPanel` (Image de fondo oscuro, a pantalla completa, **desactivado**) → hijo `CardContainer` con `HorizontalLayoutGroup`.
2. Crear 2 prefabs de card: `ModifierCardNormal` y `ModifierCardOther`. Cada uno con `Button`, `ModifierCardUI` y, en sus campos, `nameText` (TMP), `descriptionText` (TMP) e `iconImage` (Image).
3. Agregar `ModifierPanelSelection` en un GameObject activo (no dentro del panel desactivado). Asignar `panel` = ModifierPanel, `cardContainer` = CardContainer, `cardNormalWorldPrefab` y `cardOtherWorldPrefab`.
4. En `Managers/Game Modifier` → `ModifierWaveTrigger.panel`: asignar ese `ModifierPanelSelection`. Si queda vacío, lo busca solo.
5. Opcional: asignar un `icon` (Sprite) en los 30 assets de `Assets/Scriptable Object/Modifiers/`.

### GameScene — HUD de modificadores activos (MD 04)
1. GameObject con `ActiveModifiersHUD` → `iconContainer` (con `HorizontalLayoutGroup`, por ejemplo arriba a la izquierda), `iconPrefab` (un `Image` de 40×40 con Raycast Target activado), `tooltipRoot` (panel desactivado), `tooltipTitle` y `tooltipDescription` (TMP).

### GameScene — Opciones extra y reroll (MD 04)
1. `ShiftingWorldUI.tileButtons`: agregar un 4.º botón (duplicar uno de los 3) para N02. Hacer lo mismo en `turretButtons` para O02.
2. Botón **Reroll** en el panel de tiles → asignarlo en `ShiftingWorldUI.tileRerollButton` y su TMP en `tileRerollLabel`. Se oculta solo si no hay rerolls.

### GameScene — Niebla (MD 05)
1. Opcional: prefab `FogTile` con la raíz cubriendo 1×1 en XZ (por ejemplo un hijo Quad con rotación X 90), **sin collider**, layer `Fog`, material con GPU instancing. Asignarlo en `FogOfWar → FogOfWarSystem.fogTilePrefab`.
2. Revisar `heightOffset` (0.3) y los materiales `M_Blue` / `M_Red`, que hoy son los del plano viejo.

### GameScene — Zonas de recompensa (MD 06)
1. Prefab `RewardZone`: GameObject con `RewardZone` y `LineRenderer`, material unlit amarillo emisivo en `borderMaterial`, y un hijo `SpriteRenderer` asignado a `icon`.
2. En `RewardZones → RewardZoneSystem`: asignar `zonePrefab` e `icons` (un sprite por `RewardType`). Opcionales: `toastText` (TMP en el HUD), `claimVfxPrefab`, `claimAudioSource` + `claimClip`.

### Menu — Progression y Loadout (MD 13)
- Los paneles se construyen en runtime. Si querés un diseño propio, reemplazá `ProgressionPanelUI` / `LoadoutPanelUI` por prefabs.
- Las cámaras `CameraLab` y los objetos `MapAnimationLab` / `MapAnimationWorkshop` siguen en la escena con sus nombres viejos.

### Menu — Mejoras General (MD 07, obsoleto)
1. Duplicar el panel de la lista Laboratory (o Workshop) → en su `UpgradeListUI` poner `category = General`, mismo `repository`. Conectar `UpgradeDetailUI.listUI` a esa lista, o duplicar también el detalle.
2. Agregar un botón en el menú para abrir ese panel, igual que Laboratory / Workshop.

### Panel de tile seleccionado (MD 03)
- No existe un panel como `TurretInfoPanel` para tiles. El nivel y el progreso se ven en las opciones del panel de tiles. Si querés un panel al clickear un tile colocado, es una tarea nueva.

### GameScene — Botones de opción (MD 08)
1. Ya armé un layout base en los 6 botones: preview de 64 px arriba, nombre chico, "Lv X", badge "LEVEL UP!" en la esquina y "+N cells" arriba a la izquierda. El MD sugiere un preview de 96–128 px: si agrandás el botón, agrandá `Preview`.
2. Revisar los 5 íconos de `Assets/Sprites/TurretIcons/`. Para regenerarlos: **Tools/Shadow Realms/Bake Turret Icons (Force)**.
3. Opcional: ajustar colores en `Assets/Scriptable Object/Tiles/TileIconPalette.asset` y el material `Assets/Materials/Ghost/M_Ghost.mat`.

### Niebla — disolve real (MD 09 E)
1. Abrir `Assets/Shader/Clouds.shadergraph`. Activar **Alpha Clipping** en Graph Settings.
2. Agregar las propiedades `_Dissolve` (Float, 0..1) y `_DissolveDirection` (Vector3), con esos reference names exactos.
3. Alpha Clip Threshold = `_Dissolve`. Alpha = `Simple Noise(UV × 8)` mezclado con `Dot(Position(World).xz − centro, _DissolveDirection.xz)` normalizado.
4. Con eso, `FogOfWarSystem` usa el disolve del shader en lugar de la animación de escala.

### Torretas (MD 09 C)
1. En cada prefab de `Assets/Prefabs/Turrets/`, asignar `TurretShotFeedback.barrel` a la pieza que debería retroceder (hoy solo la Tesla la detecta sola).

### Torretas nuevas (MD 10)
1. Reemplazar los modelos provisorios por modelos propios cuando los haya: en cada prefab `Turret<Nombre>Prefab`, cambiar los hijos visuales y mantener los componentes. Volver a correr **Bake Turret Icons (Force)**.
2. Opcional: asignar `TurretShotFeedback.barrel` en Mortar, Ballista, Guillotine y Reaper para el retroceso.

### Audio (MD 09 B y E)
1. Opcional: asignar clips reales en `EssenceChime.clip` (ShiftWorldManager), `RewardZoneSystem.claimClips` (uno por `RewardType`), `LevelUpFeedback.levelUpClip` y `ShiftingWorldAudio.unavailableClip`. Sin clip, se usan tonos generados.
