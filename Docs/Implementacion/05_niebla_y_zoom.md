# 05 — Niebla (fog tiles) y zoom con la rueda del mouse

GDD: 2.5.1 Fog of war y 2.5.4 Camera.

## Objetivo

- Todo lo que está fuera del camino construido queda cubierto por **fog tiles** del mismo tamaño que un tile. El jugador no ve el resto del mapa.
- Cuando se coloca un tile sobre la niebla, **la reemplaza**: los fog tiles que tapa desaparecen.
- La niebla no bloquea la colocación de tiles ni de torretas.
- Con la rueda del mouse se hace zoom in y zoom out. Con zoom out se ven los bordes amarillos de las zonas de recompensa (MD 06).

## Estado actual

- `Assets/Scripts/Fog/FogWorldMaterialSwitcher.cs`: cambia el material de **un** renderer (la niebla actual, que es un plano) según el mundo. `ShiftingWorldMechanic` le llama `SetWorld(isOther)`.
- `Assets/Scripts/Tiles/Grid/GridGenerator.cs`:
  - Los tiles son `TileLayout` de `gridWidth × gridHeight` celdas de `cellSize`, encadenados por exits. No hay una grilla global alineada.
  - Cada tile colocado es un `PlacedTile` con `aabbMin` / `aabbMax` (ver `PlaceTile`, ~línea 349, y `GenerateFirst`).
  - `tileGap` separa los tiles.
  - No hay ningún evento público de "tile colocado". Hoy solo se llama a `TutorialEventHub.RaiseTilePlaced()`.
- `Assets/Scripts/Camera/CameraController.cs`:
  - **El zoom ya existe**: `Mouse ScrollWheel` y las teclas Q/R cambian `cam.fieldOfView` entre `minZoom` (5) y `maxZoom` (40).
  - Hace zoom aunque el mouse esté sobre la UI.
  - No expone el nivel de zoom.
- Los turrets se colocan con raycast a la layer `Cell` (`LayoutInstantiator.SetupGrassCell`).

## Cambios

### Evento de tile colocado

1. En `GridGenerator`, agregar `public event Action<PlacedTile> OnTilePlaced;`. Invocarlo al final de `PlaceTile` y de `GenerateFirst`, con el `PlacedTile` ya agregado a `chain`.
2. Agregar `public float TileFootprint`: el tamaño en mundo de un tile (`initialLayout.gridWidth * initialLayout.cellSize`, asumiendo que todos los layouts son cuadrados del mismo tamaño). Si encontrás layouts de otro tamaño, avisame.

### Fog tiles

3. Crear `Assets/Scripts/Fog/FogOfWarSystem.cs` (MonoBehaviour en GameScene):
   - Refs: `GridGenerator grid` y el prefab `fogTilePrefab` (un quad o cubo bajo con el material de niebla, **sin collider** y en una layer propia `Fog`, que el raycast de colocación ignora).
   - Config: `radiusInTiles` (por ejemplo 8), `heightOffset` y los materiales normal / other.
   - Al recibir el primer `OnTilePlaced` (el inicial), genera una grilla cuadrada de fog tiles centrada en el tile inicial. Paso = `TileFootprint + tileGap` (exponer `tileGap` como propiedad de solo lectura en `GridGenerator`). Se saltean los fog tiles que se superponen con tiles ya colocados.
   - En cada `OnTilePlaced`, desactiva (pool, no `Destroy`) todos los fog tiles cuyo AABB se superpone con `aabbMin` / `aabbMax` del tile nuevo. Una superposición parcial también cuenta: se borra el fog tile entero.
   - Si la grilla se queda corta (un tile se coloca cerca del borde), expandirla para mantener `radiusInTiles` alrededor del tile nuevo.
   - Evento `OnFogRevealed(int count)`. Para N11 (Fog Harvest) del MD 04: por cada fog tile revelado, sumar `GameModifiersManager.Instance.GetStat(ModifierStat.FogRevealEssence)` de esencia azul con `WorldSwitchPoints.AddBlueEssence`.
   - Escucha `WorldState.OnWorldChanged` para cambiar el material de todos los fog tiles (misma lógica que `FogWorldMaterialSwitcher`). Usar `sharedMaterial` o un `MaterialPropertyBlock` para no instanciar un material por fog tile.
4. `ShiftingWorldMechanic` mantiene su `fogWorld` actual. Si el plano de niebla viejo queda redundante con los fog tiles, avisame y lo saco de la escena.
5. Los enemigos solo caminan por tiles colocados, así que la niebla nunca los tapa. Verificar que los spawn points (`GridGenerator.GetSpawnPoints`) estén siempre sobre tiles colocados.

### Zoom

6. `CameraController`:
   - Ignorar el scroll si `EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()`.
   - Suavizar el zoom: `targetFov` + `Mathf.Lerp` con `zoomSmoothing`, usando `Time.unscaledDeltaTime` para que funcione con el juego pausado o acelerado.
   - Exponer `public float ZoomNormalized` (0 = máximo zoom in, 1 = máximo zoom out) y `public event Action<float> OnZoomChanged`.
   - Ajustar `minZoom` y `maxZoom` para que con zoom out se vea toda la grilla de niebla de `radiusInTiles`. Dejarlos serializados, sin hardcodear.
   - Mantener Q/R.
7. Verificar que `CameraController` esté en la cámara de GameScene y que no lo pise un Cinemachine. Si hay Cinemachine, aplicar el zoom sobre su lens en lugar de `Camera.main`.

## Criterios de aceptación

- Al empezar la partida, alrededor del tile inicial hay niebla en todas las direcciones.
- Colocar un tile hace desaparecer la niebla que tapa. El resto sigue.
- Se pueden colocar tiles y torretas igual que antes (la niebla no intercepta clicks).
- Al cambiar de mundo, la niebla cambia de material.
- La rueda del mouse hace zoom suave. Sobre un panel de UI no hace zoom.
- Con N11 activo, revelar niebla suma esencia azul.
- Sin caídas de FPS notables con la grilla completa (medir con el Profiler; si hace falta, usar GPU instancing en el material).
