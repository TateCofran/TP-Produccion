# 08 — Previews en la selección y en la colocación de tiles y torretas

Depende de los MD 03 (nivel de tiles), 05 (`OnTilePlaced`, fog) y 06 (zonas de recompensa).

## Objetivo

- Los botones de selección de **tiles** y **torretas** muestran un **preview visual** en lugar del nombre. El nombre queda como texto chico debajo, junto con el nivel y las copias.
- Al **colocar**:
  - **Tile**: al pasar el mouse por una salida se ve un **fantasma del tile** en su posición real, con la rotación y el flip que se van a usar.
  - **Torreta**: un **fantasma de la torreta** sigue al mouse sobre la celda, con su rango.

## Estado actual

- `Assets/Scripts/SwitchMechanic/ShiftingWorldUI.cs`:
  - `tileButtons` / `tileLabels` y `turretButtons` / `turretLabels` son arrays fijos de 3.
  - `BindButton(...)` solo setea el texto: `layout.name` para tiles y `GetTurretDisplayName(so)` para torretas.
  - Colocación de tiles: `OnChooseTile` → `EnterTilePlacementMode` → `CreateExitButtons()` instancia `exitButtonPrefab` en `exitButtonsCanvas`, en cada salida de `grid.GetAvailableExitsFor(layout)`, con el label de la salida como texto. Al hacer click: `OnExitButtonClicked` → `grid.AppendNextUsingSelectedExitWithLayout`.
  - Colocación de torretas: `HandleTurretPlacementMode()` hace raycast a `cellLayers` y `CellSlot.TryPlace` al hacer click. No hay preview mientras movés el mouse.
  - `DelayedTurretSelectFlow` espera **1.5 s** con `WaitForSeconds` antes de ocultar el panel, y `CloseTurretPanelDelayed` otros 1.5 s. Se siente lento.
  - **Bug:** `OnChooseTurret` llama a `HandleDupeSystem(so)` **antes** de colocar. Si cancelás con click derecho, el duplicado ya se sumó igual.
- `GridGenerator.cs`:
  - `TryFindPlacement(...)` calcula layout, rotación, flip, origen y AABB, pero es `private`.
  - `GetPlacementPreviews()` existe, pero solo para la salida seleccionada y sin geometría.
- `LayoutInstantiator` (`Tiles/Services/LayoutInstatiator.cs`) instancia celdas con prefabs de pasto o camino y configura `CellSlot` / `PathCellEffect`.
- `TurretDataSO` no tiene ícono.
- `TurretRangeVisualizer.cs` ya dibuja el rango.

## Cambios

### A. Preview en los botones de tiles

1. **`Tiles/UI/TileIconGenerator.cs`** (clase estática):
   - `Texture2D GetIcon(TileLayout layout)` genera una textura de `gridWidth × gridHeight` celdas, de 16 px cada una, con `FilterMode.Point`. Cachearla por layout en un diccionario.
   - Colores (configurables con un ScriptableObject `TileIconPalette`):
     - pasto: gris verdoso oscuro;
     - camino Basic: piedra clara;
     - camino DPS: rojo;
     - Slow: celeste;
     - Stun: amarillo.
   - El efecto de cada celda sale de `layout.GetPathModifiers(cell)`.
   - Marcadores: celda de entrada con un borde blanco y salidas con un punto blanco.
   - Se genera en runtime para que siempre coincida con los datos del layout, aunque se edite el asset.
2. **`Tiles/UI/TileOptionButton.cs`** (en cada botón de `tileButtons`):
   - Refs: `RawImage preview`, `TMP_Text nameLabel`, `TMP_Text levelLabel`, `TileDupeBarUI dupeBar` (opcional) y `GameObject levelUpBadge`.
   - `Bind(TileLayout layout, TileLevelData level, Action onClick)`:
     - preview = ícono;
     - nombre chico;
     - `"Lv X"`;
     - barra de copias;
     - badge visible si esta copia sube de nivel (`dupesRequiredForNextLevel - currentDupes == 1`).
   - Tooltip al hover con el efecto y su valor actual con el nivel, por ejemplo `"Slow 30%"`.
3. **`ShiftingWorldUI.BuildNormalOptions()`**: en lugar de `BindButton`, si el botón tiene `TileOptionButton` usar `Bind`. Si no lo tiene, caer al texto como hoy. Mantener `tileLabels` por compatibilidad.

### B. Preview en los botones de torretas

4. **`TurretDataSO`**: agregar `public Sprite icon;`.
5. **Editor**: `Tools/Shadow Realms/Bake Turret Icons` en `Assets/Scripts/Editor/TurretIconBaker.cs`:
   - Por cada `TurretDataSO` del `TurretDatabaseSO`, instancia el prefab en una escena de preview.
   - Cámara ortográfica a 3/4, fondo transparente, luz direccional.
   - Renderiza a un `RenderTexture` de 256×256, guarda un PNG en `Assets/Sprites/TurretIcons/<id>.png`, lo importa como Sprite y lo asigna a `icon`.
   - Si `icon` ya está asignado a mano, no lo pisa (salvo con un flag `force`).
6. **`Turret/UI/TurretOptionButton.cs`**: igual que `TileOptionButton`. `Image preview` = `so.icon`, más nombre, `"Lv X"`, barra de copias (reusar `DupeBarUI`), badge de level up y tooltip con daño, rango y cadencia (de `TurretStats` con el nivel actual) y la afinidad de mundo.
7. **`ShiftingWorldUI.BuildOtherOptions()`**: usar `TurretOptionButton.Bind`, con el mismo fallback que tiles.

### C. Fantasma al colocar un tile

8. **`GridGenerator`**: agregar una API pública que reuse `TryFindPlacement`, sin duplicar lógica:
   - `bool TryGetPlacementForExit(TileLayout layout, string exitLabel, out TilePlacementInfo info)`.
   - `TilePlacementInfo` (struct nuevo en `Tiles/Grid/DataStructures.cs`): `layout`, `rotSteps`, `flip`, `origin`, `aabbMin`, `aabbMax`.
9. **`Tiles/Ghost/TileGhostPreview.cs`** (MonoBehaviour en GameScene):
   - `Show(TilePlacementInfo info)` instancia el layout con un **material fantasma**: transparente, con un tinte por efecto y un borde con fresnel. Va en una layer `Ghost`, **sin colliders, sin `CellSlot` ni `PathCellEffect`**.
   - Usar un pool propio o un `ILayoutInstantiator` alternativo (`GhostLayoutInstantiator`) que use los mismos prefabs de celda y que, al instanciar, saque o desactive colliders y componentes de gameplay. Cachear el fantasma por layout para no reinstanciar en cada hover.
   - `Hide()`.
   - Animación de entrada: escala 0.9 → 1 y alpha 0 → 0.6 en 0.12 s, con tiempo unscaled.
   - Si `info.aabb` se superpone con una zona de recompensa (MD 06), su borde se ilumina y aparece el ícono de la recompensa arriba del fantasma con el texto `"Reward!"`.
   - Si el modificador N11 está activo, mostrar `"+N"` con los fog tiles que se revelarían. `FogOfWarSystem` expone `CountFogOverlapping(min, max)`.
10. **Botones de salida** (`CreateExitButtons`):
    - Reemplazar el label de texto por una flecha que apunta en la dirección de salida, rotada según `dirOut`, más el mini ícono del tile elegido (`TileIconGenerator`).
    - Agregar un `ExitButtonHover` (`IPointerEnterHandler` / `IPointerExitHandler`): enter → `TileGhostPreview.Show(...)` para esa salida; exit → `Hide()`.
    - Teclado: Tab recorre las salidas y muestra el fantasma; Enter confirma; click derecho / Escape cancela y vuelve al panel (hoy está `CancelTilePlacement`).
    - Al confirmar, ocultar el fantasma antes de llamar a `AppendNextUsingSelectedExitWithLayout`.

### D. Fantasma al colocar una torreta

11. **`Turret/Ghost/TurretGhostPreview.cs`**:
    - Mientras `turretPlacingMode` está activo, cada frame hace raycast igual que `HandleTurretPlacementMode`.
    - Si toca un `CellSlot`, muestra un fantasma del prefab de la torreta, instanciado una sola vez con el material fantasma, sin `Turret` activo ni colliders, en la misma posición que calcula `CellSlot.TryPlace` (extraer ese cálculo a un método público `GetPlacementPose(GameObject prefab)` en `CellSlot`).
    - Tinte verde si la celda está libre y rojo si está ocupada.
    - Mostrar el rango con `TurretRangeVisualizer` o un círculo propio, usando el rango con el nivel actual.
    - Ocultarlo al salir de las celdas, al colocar o al cancelar.
12. **Arreglos en `ShiftingWorldUI`**:
    - Mover `HandleDupeSystem(so)` al momento en que `slot.TryPlace` devuelve `true`. Cancelar no suma duplicado.
    - Bajar las esperas de `DelayedTurretSelectFlow` y `CloseTurretPanelDelayed` a 0 (cerrar en el acto), o como máximo a una animación de 0.15 s con tiempo unscaled.

### E. Editor (checklist para mí)

- Crear las layers `Ghost` y `Fog`, y excluirlas de `cellLayers`.
- Material fantasma (URP Unlit transparente con fresnel) y el asset `TileIconPalette`.
- Cambiar el layout de los botones de opción: imagen arriba de 96–128 px, nombre y nivel abajo, badge en la esquina.
- Correr `Bake Turret Icons` y revisar los 5 sprites.

## Criterios de aceptación

- El panel de tiles muestra 3 imágenes de los layouts, con los colores de su efecto, y no solo sus nombres. El nombre y el nivel se ven chicos debajo.
- El panel de torretas muestra los íconos de las torretas.
- Al pasar el mouse por una salida, aparece el fantasma del tile exactamente donde va a quedar, con la misma rotación. Al confirmar, el tile real coincide con el fantasma.
- El fantasma no se puede clickear, no bloquea raycasts y no afecta a enemigos.
- Al colocar una torreta, el fantasma sigue al mouse por las celdas: verde en las libres, rojo en las ocupadas, con el rango visible.
- Cancelar la colocación de una torreta no suma duplicado.
- El panel de torretas se cierra en el acto al colocar.
