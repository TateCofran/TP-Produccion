# Prompt: implementación de los cambios del GDD (Shadow Realms)

Copiá todo este archivo como prompt en Claude Code, con el proyecto `D:/Unity/TP-Produccion` abierto.

---

Sos el desarrollador de **Shadow Realms**, un tower defense en Unity 6000.3.12f1 (proyecto `TP-Produccion`, rama `main`). El GDD oficial está en Notion ("GDD — Game Design Document") y ya tiene aplicados los cambios de diseño del 06/11/2026. Tu trabajo es implementarlos en el código.

Las especificaciones están en `Docs/Implementacion/`, una por feature. Implementalas **en este orden**, una por vez:

1. `01_esencias_por_kill.md`
2. `02_sin_tope_de_nivel.md`
3. `03_duplicados_tiles.md`
4. `04_modificadores.md`
5. `05_niebla_y_zoom.md`
6. `06_zonas_de_recompensa.md`
7. ~~`07_mejoras_permanentes.md`~~ **Reemplazado por el MD 13.** No lo implementes; si ya está implementado, el MD 13 lo elimina.
8. `08_previews_seleccion.md`
9. `09_game_feel_y_pulido.md` (por bloques A → E; el F solo si te lo pido)
10. `10_torretas_nuevas.md`
11. `11_variantes_tiles.md`
12. `12_sinergias_elites_bounty_legendarios.md` (por bloques A → D)
13. `13_meta_progresion_y_loadout.md`

Los MD 01 a 09 ya están implementados casi por completo. Antes de arrancar el 10, revisá rápido que cada uno cumpla sus criterios de aceptación y pasame una lista de lo que falte.

## Cómo trabajar

- Antes de empezar cada MD, leelo completo y abrí todos los archivos que nombra. Las rutas y los nombres de clases de los MD salen del código actual, pero verificá que sigan igual antes de editar.
- Al terminar cada MD:
  1. El proyecto tiene que compilar sin errores ni warnings nuevos.
  2. Revisá la sección "Criterios de aceptación" del MD y decí cuáles verificaste y cómo.
  3. Hacé un commit por MD, con el mensaje `feat: <nombre del MD>`.
  4. Pará y esperá mi confirmación antes de pasar al siguiente. Yo lo pruebo en Play Mode.
- Si un MD choca con algo que encontrás en el código (otra clase que ya lo hace, una dependencia que no figura), no lo resuelvas por tu cuenta: explicame el conflicto y proponé una opción.
- Lo que un MD marca como **Pendiente de decisión** no se implementa. Dejalo como está y avisame.

## Convenciones del proyecto (obligatorias)

- Todo el código en inglés: clases, métodos, variables y textos de UI.
- **Sin comentarios** en el código y **sin XML summaries** (`///`).
- `Debug.Log` / `LogWarning` / `LogError` siempre con prefijo `[ClassName]`, sin emojis ni símbolos especiales. Ejemplo: `Debug.Log("[KillEssenceRewarder] Granted 3 blue essences")`.
- No renombres ni muevas clases `MonoBehaviour` o `ScriptableObject` que ya están en escenas o prefabs: se rompe la referencia al script. Si un campo serializado cambia de nombre, usá `[FormerlySerializedAs("oldName")]`.
- Seguí el estilo existente: `[SerializeField] private`, `[Header]`, `[Tooltip]`, eventos `System.Action`, singletons con `Instance` donde ya los hay.
- El input usa el Input Manager viejo (`Input.GetAxis`, `Input.GetKeyDown`). No migres a Input System en estas tareas.
- La pausa del juego se hace con `GameTime.RequestPause()` / `GameTime.ReleasePause()`, no tocando `Time.timeScale` directamente.
- El modo activo se lee de `WorldState.Current` (`ShiftingWorldMechanic.World.Normal` = modo tiles, `World.Other` = modo torretas). Los cambios de modo se escuchan con `WorldState.OnWorldChanged`.

## Cambios que tenés que respetar en todos los MD

- No hay oro. Las esencias son el único recurso y, desde el MD 13, son la XP de la meta-progresión (no se gastan).
- Antes de cada partida el jugador arma un mazo de 3 a 5 torretas y de 3 a 5 tiles. En la partida solo salen cosas del mazo (MD 13).
- Los tiles controlan (slow, stun, efectos) y las torretas hacen daño. Ninguna torreta nueva aplica slow ni stun.
- La carpeta de ScriptableObjects es `Assets/Scriptable Object/` (con espacio).
- No hay tope de nivel ni en torretas ni en tiles.
- Hay 2 modos: Normal (construye tiles) y Other World (construye torretas).
- Si un cambio de escena o prefab no se puede hacer desde código, dejá un checklist con los pasos exactos que tengo que hacer en el Editor: qué GameObject, qué componente, qué campo.
