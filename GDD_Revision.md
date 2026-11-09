# Revisión GDD vs Proyecto — Shadow Realms

Fecha: 2026-11-06 (actualizada después de implementar los MD 01–09)
GDD de referencia: **GDD Shadow Realms - Beta.docx** (Google Drive, 27/11/2025). Es la versión más nueva y la que coincide con el proyecto: dos modos (tiles / torretas), sin oro.
Proyecto: `D:/Unity/TP-Produccion`, Unity 6000.3.12f1, rama `main`.

> El GDD de Notion ("GDD — Game Design Document") es una versión **anterior**: tiene oro, corrupción, costos de torretas y nivel del core. No refleja el juego actual (ver sección 5).

Leyenda: ✅ hecho · 🟡 a medias / con problemas · ❌ falta · ⚪ fuera del alcance de la Beta

---

## 1. Resumen de lo pendiente (por prioridad)

| # | Estado | Tema | Qué falta |
|---|---|---|---|
| 1 | ✅ **Corregido** | Progenitor (MiniBoss zona 3) | El prefab usaba los datos del Vampiro común. Corregido el 2026-11-06 (detalle en 2.3). |
| 2 | 🟡 **Implementado (MD 04)** | Modificadores / eventos cada 3 rondas | 30 modificadores (15 Normal + 15 Other), trigger cada 3 oleadas, cards animadas (MD 09). **Falta armar el panel y el HUD en el Editor** (`Docs/Implementacion/REPORTE_IMPLEMENTACION.md`). |
| 3 | ✅ **Decidido (MD 02)** | Tope de nivel de torretas = nivel del core | El diseño cambió: no hay tope de nivel en torretas ni en tiles. Actualizar el GDD. |
| 4 | ✅ **Implementado (MD 01)** | Obtención de esencias | Ahora se ganan por kill (azul en Normal, roja en Other); cambiar de mundo ya no da esencias. No hay esencias por tiempo. |
| 5 | 🟡 **Implementado (MD 07)** | Mejora permanente "General" (vida inicial del core) | Core Vitality (+2 de vida por nivel). Falta la lista General en el menú (Editor). |
| 6 | 🟡 | Sistema de puntaje / rendimiento | La pantalla de resultado muestra datos, pero no hay puntaje ni bonus por rendimiento (bosses, kills rápidas). |
| 7 | 🟡 | Big Goblin "mucha vida y defensa" | `defense = 0` en **todos** los enemigos. |
| 8 | 🟡 | Splash Screen con logo | `IntroScene` muestra 3 slides de intro. Verificar que aparezca el logo. |
| 9 | 🟡 | Documentación desactualizada | Tanto el GDD Beta como el de Notion tienen restos de la versión vieja (sección 5). |

---

## 2. Detalle por sección del GDD

### 1.3 / 1.8 — USPs y Feature Set
| Feature | Estado | Dónde / notas |
|---|---|---|
| Cambio entre modos (Shift) | ✅ | `SwitchMechanic/ShiftingWorldMechanic.cs`, cooldown de 5 s |
| Construcción de caminos (3 opciones al llenar la barra) | ✅ | `Tiles/Grid/GridGenerator.cs`, `Tiles/Services/Candidates.cs` |
| Mejoras de torretas | ✅ | Sistema de duplicados: `Turret/DupeSystem/*` |
| Mejoras entre partidas | ✅ | `Upgrades/*`, menú Laboratory / Workshop |
| Sistema de puntaje y eficiencia | 🟡 | `ResultSceneController` muestra tiempo, oleadas, kills y esencias. No calcula puntaje. |
| Estética dark fantasy low poly | ✅ | `PolygonDungeonRealms`, VFX y shaders de mundo |
| Música ambiente | ✅ | `ShiftingWorldAudio` + `NWorldMusic` / `OtherWorldSound2`, etc. |

### 1.4 / 1.5 — Idioma y plataforma
- ✅ Todo el texto de UI está en inglés.
- ✅ Build: StandaloneWindows64.
- 🟡 Placeholder "Tittle" (con typo) en `Menu.unity`. Revisar si se ve en pantalla.

### 1.9 — Project Scope (Beta)
| Elemento | Beta pide | Proyecto | Estado |
|---|---|---|---|
| Oleadas | Máx. 45 | `WaveList.asset`: 45 oleadas; `wavesToWin: 45` en GameScene | ✅ |
| Enemigos | 15 (5 por zona) | 15 `EnemyData` en Zone1/2/3 | 🟡 ver 2.3 |
| Torretas | Basic, Fire, Tesla, Sniper, Energy | Las 5 en `Database.asset` | ✅ |
| Mejoras permanentes | 1 por mundo | 3 de Tiles + 4 de Torretas | ✅ (supera lo pedido) |
| Oleadas infinitas, fusión de torretas | Deseado | — | ⚪ |

> `Prefabs/UI/GameScene/GameManager.prefab` tiene `wavesToWin: 20`. La escena lo pisa con 45, pero si se reemplaza la instancia vuelve a 20.

### 2.1.1 / 2.1.2 — Core Loop y mecánicas
- ✅ La primera oleada empieza al colocar el primer tile o torreta, con botón "Start Wave".
- ✅ Al terminar la partida se acumulan las 2 esencias (`EssenceBank`) y se gastan en el menú.
- ❌ **"The cap of level up is the current level of the core, the player can upgrade to surpass this limit."** `Core.cs` no tiene nivel ni forma de subirlo. Las torretas suben por duplicados sin ese tope. Para tiles existe un tope, pero viene de una mejora permanente (`TileLevelCapProviderSO`), no del core.
  - Opciones: implementarlo, o sacarlo del GDD (2.1.2 y 2.6.9.3, "investing in core upgrades").

### 2.1.3 / 2.2 — Progresión y esencias
- ✅ Dos tipos de esencia: Normal (azul) y Other World (roja), visibles en el HUD y en el resultado.
- ✅ **Actualizado (MD 01):** las esencias se ganan por kill, del color del mundo activo (`WorldSwitchPoints.cs`), y vuelan al contador como orbes (MD 09). No hay esencias por tiempo: actualizar el texto del GDD.
- ❌ 2.1.3.5: experiencia de metajuego por rendimiento (sobrevivir más, kills rápidas, derrotar bosses, cambiar seguido). No existe.

### 2.3 — Enemigos
| GDD | Asset / prefab | Habilidad implementada | Estado |
|---|---|---|---|
| Goblin Minion | GoblinMinion / DirectEnemy | — | ✅ |
| Goblin Sprint | FastGoblin | SprintOnLowHP | ✅ |
| Big Goblin ("lot of health **and defense**", "very low speed") | BigGoblin | SpeedAura (no figura en el GDD) | 🟡 `defense: 0` |
| Mini boss Goblin (5 de daño al core) | MiniBossGoblin | ShieldOnLowHP | ✅ |
| Boss Goblin (destruye el core, solo en oleada 15) | BossGoblin, 100 de daño | SpawnMinionsPeriodic | ✅ |
| Esqueleto | MinionSkeleton | — | ✅ |
| Calavera | FastSkeleton | SprintOnLowHP | ✅ |
| Mago Esqueleto (escuda aliados) | BigSkeleton | ShieldAura | ✅ |
| Caballero negro (revive y corre) | MiniBossSkeleton | SecondLife | ✅ |
| Nigromante (escudo invulnerable) | BossSkeleton | LinkedShield | ✅ |
| Vampiro | MinionVampire | — | ✅ |
| Pesadilla | FastVampire | SprintOnLowHP | ✅ |
| Ojo Maligno (cura aliados) | BigVampire | HealAura | ✅ |
| **Progenitor** (se cura a sí mismo) | **EnemyMiniBossPrefab3** | SelfHealPeriodic + SpawnOnDeath | ✅ (bug corregido) |
| Ángel Caído (desactiva torres, con cooldown) | BossVampire | DisableTowersEMP + DestroyTurretOnLowHP | ✅ |

**🐞 Bug del Progenitor.** En `Assets/Prefabs/Enemies/Zone3/EnemyMiniBossPrefab3.prefab`, el campo `data` del componente `Enemy` apunta a `MinionVampire.asset` y no a `MiniBossVampire.asset`. `MiniBossVampire.asset` no lo usa ningún prefab. Consecuencias:
- El Progenitor aparece con 50 de vida, velocidad 1.25 y 1 de daño, en vez de 1500 / 1.7 / 5.
- `Enemy.Type` devuelve `Minion3`. Cuando muere, `EnemyPool.ReturnEnemy` lo mete en la cola de `Minion3`, así que en oleadas posteriores pueden salir Progenitores donde debían salir Vampiros comunes.
- ✅ **Corregido (2026-11-06):** el campo `data` ahora apunta a `MiniBossVampire.asset`. Verificado en Unity: vida 1500, velocidad 1.7, daño 5, `Type = MiniBoss3`.

**Otros puntos:**
- `defense` está en 0 en los 15 enemigos. El GDD (Big Goblin) y la sección de QA ("their armor amount was slightly reduced") asumen que existe armadura.
- Los nombres del GDD están en español (Esqueleto, Calavera…), pero el juego está en inglés: las descripciones dicen Black Knight, Necromancer, Progenitor, Fallen Angel. Conviene unificar los nombres en el GDD.
- La columna "Description" de la tabla de enemigos está vacía en el GDD.

### 2.4 — Torretas
| GDD | Proyecto | Estado |
|---|---|---|
| Normal Turret (poco daño, mucha cadencia) | BasicTurret, daño 7, fireRate 1 | ✅ |
| Fire Turret (daño por tick) | FireTurret + `FireTurret.cs` | ✅ |
| Generator (más energía) | EnergyTurret + `TurretEnergy.cs` | ✅. El GDD la llama "Generator" en 2.4 y "Energy Turret" en 1.9: unificar. |
| Electric Turret (varios enemigos) | TeslaTurret + `TeslaTurret.cs` | ✅. El GDD dice "Electric" en 2.4 y "Tesla" en 1.9. |
| Sniper (más daño y rango, más cooldown) | sniperTurret, daño 17, rango 10, fireRate 0.1 | ✅ |

Detalles técnicos menores:
- ✅ **Corregido (MD 04):** `TurretType` incluye `Sniper` y las 5 `TurretDataSO` tienen el campo nuevo `turretType` asignado. El campo string `type` sigue vacío.
- `Turret/TurretInfoUI.cs` es código muerto (ninguna escena ni prefab lo usa) y tiene `SellTurret()` vacío. El panel que se usa es `TurretInfoPanel`, con el botón "Remove Turret".
- La columna "Visual" de la tabla de torretas está vacía en el GDD.

### 2.5 — Path Building
- ✅ 3 opciones de tiles prearmados cuando se llena la barra.
- ➕ El proyecto tiene **tiles con efectos** (Basic, DPS, Slow, Stun, en `Scriptable Object/Tiles/*`) y niveles de tiles por duplicados. **Nada de esto está en el GDD**: falta documentarlo.

### 2.6 — Mejoras permanentes
| Categoría GDD | Proyecto | Estado |
|---|---|---|
| General: vida inicial del core | Core Vitality (`upgrade_coreHealth`, MD 07) | 🟡 falta la lista en el menú |
| Normal Mode (tiles) | Swift Builder, More Essences per kill, Treasure Seeker (MD 07) | ✅ |
| Dark Mode (torretas) | Damage, Fire Rate, Range, More Essences | ✅ |

### 2.6.4 — World Shifting
- ✅ Cambio de modo durante la oleada, con cooldown y barras de progreso por modo.
- 🟡 2.6.6 habla de *"poor management of resources or world switching increases corruption"*, pero no hay corrupción en el proyecto. Sacarlo del GDD.

### 2.6.7 — Oleadas
Composición real en `WaveList.asset`:
- ✅ 45 oleadas. Boss en la 15 (Goblin), la 30 (Skeleton) y la 45 (Vampire), "every 15 rounds". Cada bloque de 15 corresponde a una zona.
- 🟡 El texto de "General Structure" no coincide con los datos:
  - "Wave 10: first MiniBoss": ya hay un MiniBoss en la **oleada 5**.
  - "Wave 30: Five MiniBosses": hay **4**.
  - "Waves 21–29: Super units": en el proyecto no hay unidades "Super". Desde la 16 entran los enemigos de la zona 2.
  - Falta describir las oleadas 40–44.
  - **Acción:** reescribir esta sección según las 3 zonas reales.
- ⚪ El "valor interno" de cada enemigo para balancear oleadas es una herramienta de diseño. No hace falta en código.

### 2.6.8 / 2.6.9 — Rejugabilidad y objetivos
- 🟡 **Modificadores y eventos especiales cada 3 rondas** (2.6.9.3). **Implementado en el MD 04** (`ModifierSO`, 30 assets, `ModifierWaveTrigger`, cola de paneles del MD 09). Falta crear en el Editor el panel de cards (`ModifierPanelSelection`) y el HUD de activos. Actualizar 2.6.8 del GDD, que todavía dice que se quitaron.

### 3.1 — HUD
| Elemento | Estado |
|---|---|
| Arriba a la derecha: oleada, enemigos, vida, esencias por modo | ✅ textos Wave / Enemies / Health / Normal Essences / Other Essences |
| Abajo a la izquierda: stats de la torreta + mejorar + vender | 🟡 Muestra stats, nivel, modo de targeting y "Remove Turret". No hay botón de mejora porque se mejora por duplicados. Actualizar el GDD. |
| Abajo al centro: círculo de cooldown + barra de tiles (izq.) + barra de torretas (der.) | ✅ `ShiftingWorldUI` (verificar el layout en juego) |

### 3.2 — Escenas
| GDD | Proyecto | Estado |
|---|---|---|
| Splash Screen (logo) | `IntroScene` (3 slides, se muestran una sola vez según PlayerPrefs) | 🟡 Confirmar que muestre el logo |
| Main Menu (upgrade, play, exit) | `Menu` con Play, Laboratory, Workshop, Options, Tutorial y Exit | ✅ |
| Game Scene | `GameScene` | ✅ |
| Game Over / Win | `ResultScene` (Won!/Lose…, tiempo, oleadas, kills, esencias) | ✅ (sin puntaje) |

En el proyecto pero no en el GDD: **Tutorial** (`TutorialManager`), **Opciones** (audio, resolución, pantalla), **Pausa** y **control de velocidad** (`SpeedButtonGroup`). Conviene documentarlos.

### 4 — Arte / Audio
- ✅ Hay assets de arte y audio (música por mundo, SFX de flecha, fuego, rayo y clicks).
- 🟡 El índice del GDD Beta incluye **Audio → Music / Sound Effects**, pero esa sección no tiene contenido. Concept Art tampoco tiene imágenes en el texto exportado.

### 5 — Producción / QA
- La sección de QA dice que *"the Big enemy… their armor amount was slightly reduced"*, pero la armadura es 0 en todos.
- 🟡 Aviso en la consola de Unity: el proyecto usa el **Input Manager**, que está marcado como obsoleto. No bloquea, pero conviene migrar a Input System.

---

## 3. Lo que está en el proyecto pero no en el GDD
- Tiles con efectos (DPS / Slow / Stun) y niveles de tiles por duplicados (MD 03), sin tope de nivel (MD 02).
- Niebla de guerra alrededor del camino y zoom suave (MD 05).
- Zonas de recompensa dentro de la niebla (MD 06).
- Previews visuales en la selección de tiles y torretas, y fantasmas al colocar (MD 08).
- Game feel (MD 09): cola de paneles, orbes de esencia, hit flash, números de daño, VFX de muerte por zona, banner y barra de boss, shake, viñeta del core, disolve de niebla, level up visual, efectos de cambio de mundo y animación de colocación de tiles.
- Afinidad de mundo en enemigos (`vulnerableIn`) y torretas (`affinity`, `affinityBonus`, `offWorldPenalty`).
- Tutorial interactivo, menú de pausa, opciones, velocidad de juego.
- Ciclo día/noche (`DayNightController`), niebla por mundo y pulso visual al cambiar.
- Habilidades extra de enemigos que el GDD no menciona: SpeedAura (Big Goblin), ShieldOnLowHP (MiniBoss Goblin), SpawnMinionsPeriodic (Boss Goblin), SpawnOnDeath (Progenitor), DestroyTurretOnLowHP (Ángel Caído).

---

## 4. Checklist de tareas

**Código / proyecto**
- [x] Asignar `MiniBossVampire.asset` en `EnemyMiniBossPrefab3.prefab` (bug del Progenitor).
- [ ] Decidir: implementar modificadores o eliminarlos del GDD y del código.
- [ ] Decidir: implementar nivel del core como tope de torretas o sacarlo del GDD.
- [ ] Esencias por kill y/o por tiempo en cada mundo.
- [ ] Mejora permanente General: vida inicial del core.
- [ ] Puntaje de rendimiento en `ResultScene`.
- [ ] Valores de `defense` para los enemigos Big (o sacar armadura del GDD).
- [ ] Confirmar el logo en `IntroScene`.
- [ ] Limpieza: borrar `TurretInfoUI.cs` (sin uso), agregar `Sniper` a `TurretType`, poner `wavesToWin: 45` en el prefab `GameManager`, corregir el placeholder "Tittle".

**Documento**
- [ ] Reemplazar el GDD de Notion por la versión Beta (o marcarlo como obsoleto).
- [ ] Quitar de la Beta las menciones a corrupción, oro, core upgrades y modificadores (si no se implementan).
- [ ] Reescribir 2.6.7 "General Structure" según las 3 zonas reales.
- [ ] Unificar nombres de torretas (Generator/Energy, Electric/Tesla) y de enemigos (ES/EN).
- [ ] Completar las columnas vacías (Description de enemigos, Visual de torretas) y la sección Audio.
- [ ] Documentar tiles con efectos, afinidades, tutorial, pausa, opciones y velocidad.

---

## 5. Diferencias del GDD de Notion (versión vieja) con el proyecto
Están en Notion y **no** en el proyecto: economía de **oro** (costo de torretas, "Fast Turret 15 gold", 1270 de oro hasta nivel 10), **corrupción**, **nivel del core**, mejoras de tipo "lower corruption" / "extra gold per wave", **modificadores**, y el mensaje "You must upgrade the core to upgrade this further". El concepto de "dos mundos con enemigos y recursos distintos" cambió en la Beta a "dos modos: tiles vs torretas". Si la Beta es la versión oficial, conviene actualizar o archivar la página de Notion.
