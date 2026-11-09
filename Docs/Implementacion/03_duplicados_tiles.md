# 03 — Duplicados de tiles con la misma lógica que las torretas

GDD: 2.5.2 Tile levels (duplicates).

## Objetivo

- Los tiles suben de nivel al colocar copias, con **la misma lógica y las mismas copias por nivel que las torretas**.
- Un tile cuenta como duplicado solo si es **exactamente el mismo**: misma forma y mismo efecto. En el código eso es **el mismo asset `TileLayout`**. La rotación y el flip no cambian la pieza: un `TileLayout` rotado sigue siendo el mismo tile.
- Leveling activo desde el inicio, sin mejora permanente que lo desbloquee.
- Sin tope de nivel (ver MD 02).
- El nivel aumenta la fuerza del efecto del tile.

## Estado actual

- `Assets/Scripts/Tiles/Dupes/TileDupeSystem.cs`: diccionario por `TileDataSO.id`.
- `Assets/Scripts/Tiles/Grid/GridGenerator.cs`:
  - `AttachAndApplyDupe(parent, layout)` (línea ~872) resuelve un `TileDataSO` con `tileDataBindings` y, si no encuentra, usa **`defaultTileData`**. **Bug:** todos los layouts sin binding comparten la misma clave, así que un tile DPS y uno Slow cuentan como duplicados entre sí.
  - Se llama tanto en `PlaceTile` como en `GenerateFirst` (el tile inicial con el core también suma un duplicado).
- `Assets/Scripts/Tiles/Dupes/TileLevelData.cs`:
  - Arranca con `dupesRequiredForNextLevel = 1`. Las torretas (`TurretLevelData` en `Turret/DupeSystem/ITurretDupeSystem.cs`) arrancan en **2**. Los dos crecen ×1.5 con `CeilToInt`.
  - `LevelUp()` no cambia los multiplicadores, así que subir de nivel no hace nada.
  - Bloqueado por `ITileLevelingPermissionProvider` / `TileLevelingPermissionProvider` (mejora "Tile Dupe System").
- `Assets/Scripts/Tiles/Dupes/TileLevelApplier.cs` + `TileStats.cs`: aplican `value / spawnChance / size` de `TileDataSO`, valores que **no usa** ningún efecto.
- El efecto real del tile está en `Assets/Scripts/Tiles/PathCellEffect.cs` (`damagePerHit`, `slow`, `stun`, seteados por `LayoutInstantiator.SetupPathCell`) y pasa por `PathEffectManager.GetEffective(...)`.
- `PathEffectManager` declara `slowMax` y `stunMax`, pero `GetEffective` **no los aplica**. Sin tope de nivel, eso hace falta.
- UI:
  - `TileDupeUI.cs` y `TurretDupeUI.cs` son singletons con `DontDestroyOnLoad` que cachean datos por nombre.
  - `TileDupeUI` se conecta al sistema solo en `Start()`. Después de recargar `GameScene`, queda apuntando a un `TileDupeSystem` destruido y muestra niveles de la partida anterior.
  - `TileDupeBarUI.cs` dibuja la barra buscando por el texto del label.

## Cambios

1. **Progresión compartida**: crear `Assets/Scripts/Common/DupeProgression.cs`, una clase `[Serializable]` con `currentLevel`, `currentDupes`, `dupesRequiredForNextLevel` (inicial 2), `AddDupe()` → `bool leveledUp` y `GetStatusText()`. El crecimiento ×1.5 con `CeilToInt` tiene que quedar en un solo lugar.
   - `TurretLevelData` y `TileLevelData` usan `DupeProgression`. Mantener sus campos públicos, o propiedades con el mismo nombre, para no romper `TurretDupeUI`, `TurretLevelApplier` ni `DupeBarUI`.
   - Los multiplicadores de cada uno se recalculan en función de `currentLevel`.
2. **Clave por layout**: cambiar `ITileDupeSystem` / `TileDupeSystem` para que trabajen con `TileLayout` en vez de `TileDataSO`:
   - `GetTileLevelData(TileLayout layout)`, `AddDupe(TileLayout layout)` y los eventos `OnTileLevelUp(TileLayout, TileLevelData)` / `OnDupeChanged(TileLayout)`.
   - Clave: `layout.name`. Si dos assets pudieran tener el mismo nombre, usar la referencia del asset en el diccionario.
   - Agregar `ResetAllLevels()` al iniciar la partida.
3. **Quitar el bloqueo**: borrar el chequeo de `_levelingProvider` en `TileLevelData`, `SetLevelingProvider`, `ITileLevelingPermissionProvider.cs`, `TileLevelingPermissionProvider.cs` y su búsqueda en `TileDupeSystem.Awake()`. Antes, verificar que no esté en escenas o prefabs (mismo procedimiento que el MD 02).
4. **`GridGenerator.cs`**:
   - `AttachAndApplyDupe(parent, layout)` pasa el `layout` directo al dupe system. Borrar `ResolveTileData`, `defaultTileData`, `tileDataBindings` y la clase `TileDataBinding`.
   - En `GenerateFirst`, el tile inicial **no** suma duplicado: no llamar a `AddDupe` para `initialLayout`, pero sí agregarle el `TileLevelApplier` en nivel 1.
5. **Aplicar el nivel al efecto**:
   - `TileLevelData`: multiplicadores `damageMultiplier = 1 + 0.2 * (level - 1)`, `slowMultiplier = 1 + 0.1 * (level - 1)` y `stunMultiplier = 1 + 0.1 * (level - 1)`.
   - `TileLevelApplier`: en `Initialize(TileLayout)` toma el level data, se suscribe a `OnTileLevelUp` (filtrando por su layout, igual que `TurretLevelApplier`) y empuja los multiplicadores a todos los `PathCellEffect` hijos de su `parent`. Así, **todas las copias del mismo layout comparten nivel**, como las torretas del mismo tipo.
   - `PathCellEffect`: guardar los valores base que recibe en `Setup(...)` y agregar `SetLevelMultipliers(float dmg, float slow, float stun)`. Los valores efectivos son base × multiplicador.
   - `PathEffectManager.GetEffective`: aplicar `outSlow = Mathf.Min(outSlow, slowMax)` y `outStun = Mathf.Min(outStun, stunMax)`.
   - Borrar `TileStats.cs`, `TileDataSO.cs` y los assets `TileData` si no los usa nadie más.
   - **Pendiente de decisión:** los tiles Basic no tienen efecto, así que subirlos de nivel no cambia nada. Mostrar el nivel igual y avisame.
6. **UI**:
   - `TileDupeUI`: clave = `layout.name`. Sacar `DontDestroyOnLoad`, o suscribirse a `SceneManager.sceneLoaded` para reconectarse al `TileDupeSystem` nuevo y limpiar el diccionario. Hacer lo mismo en `TurretDupeUI`.
   - `ShiftingWorldUI.BuildNormalOptions()`: el label del botón de cada opción tiene que mostrar el nivel actual del layout y el progreso, por ejemplo `"{layout.name}  Lv 2 (1/3)"`. Si hay un `TileDupeBarUI` en el botón, actualizarlo con ese layout.
   - Panel de tile seleccionado (si existe, como `TurretInfoPanel` para torretas): nivel y copias para el siguiente nivel. Si no existe, que quede como tarea del Editor en el checklist.

## Criterios de aceptación

- Colocar el mismo `TileLayout` 2 veces lo sube a nivel 2, igual que con 2 torretas del mismo tipo. Los requisitos de copias son iguales en tiles y torretas para cada nivel.
- Un layout DPS y un layout Slow no cuentan como duplicados entre sí. Dos layouts con el mismo efecto y distinta forma, tampoco.
- Al subir de nivel un layout DPS, todas sus copias colocadas hacen más daño.
- Slow nunca pasa de `slowMax` y stun nunca pasa de `stunMax`.
- Al reiniciar la partida, todos los tiles y torretas vuelven a nivel 1 en la lógica y en la UI.
- No hace falta comprar ninguna mejora para que los tiles suban de nivel.
