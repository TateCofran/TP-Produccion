# 02 — Sin tope de nivel (torretas y tiles)

GDD: 2.4.1 y 2.5.2.

## Objetivo

Ni las torretas ni los tiles tienen nivel máximo. Se borra todo el código de tope.

## Estado actual

- `Assets/Scripts/Turret/Data/TurretStats.cs`:
  - `[SerializeField] private int maxUpgradeLevel = 15;` (línea 10)
  - `public int MaxUpgradeLevel => maxUpgradeLevel;` (línea 38)
  - En `SetLevel(...)`: `upgradeLevel = Mathf.Clamp(level, 1, maxUpgradeLevel);`. **Este es un tope oculto en 15**, aunque el sistema de duplicados siga sumando.
- Tope de tiles (asociado a la mejora permanente "Lvl Cap"):
  - `Assets/Scripts/Tiles/ITileLevelCapProvider.cs`
  - `Assets/Scripts/Tiles/TileLevelCapProvider.cs`
  - `Assets/Scripts/Tiles/TileLevelCapProviderSO.cs`
  - `Assets/Scripts/Tiles/TileLevelCapConfig.cs` (ScriptableObject `Tiles/LevelCapConfig`)
- Hoy nadie consulta `GetMaxTileLevel()` ni `GetCurrentCap()`. El tope está declarado pero no se aplica, salvo el bloqueo de `TileLevelingPermissionProvider`, que se trata en el MD 03.

## Cambios

1. **`TurretStats.cs`**: cambiar el clamp por `upgradeLevel = Mathf.Max(1, level);`. Borrar `maxUpgradeLevel` y `MaxUpgradeLevel`. Antes, buscar usos de `MaxUpgradeLevel` en todo el proyecto (UI de torreta, `TurretInfoPanel`, `TurretInfoUI`, barras) y sacar cualquier texto tipo "MAX" o "x/15".
2. **Tiles**: buscar referencias a las 4 clases de tope en scripts, escenas (`Assets/Scenes/*.unity`) y prefabs.
   - Si **no** están en ninguna escena ni prefab: borrar los 4 `.cs` con sus `.meta` y el asset `TileLevelCapConfig` si existe.
   - Si están en escena: borrar primero el componente de la escena (avisame con un checklist si no se puede desde código) y después los scripts.
3. **Curva sin tope**: `TurretLevelData.LevelUp()` (`Turret/DupeSystem/ITurretDupeSystem.cs`) suma +0.2 de daño, +0.1 de rango y +0.15 de cadencia, lineal por nivel. Se mantiene así. Solo hay que confirmar que ningún valor se clampea.
   - **Pendiente de decisión:** el GDD pide que la curva crezca más lento en niveles altos. No lo implementes ahora. Dejá un único punto de cambio: un método `GetLevelBonus(level)` o similar. Lo balanceamos después del playtest.

## Criterios de aceptación

- Una torreta puede pasar de nivel 15 y sus stats siguen subiendo (probarlo forzando duplicados en el Inspector o con un botón de debug temporal que no se commitea).
- No queda ninguna referencia a `TileLevelCap*` ni a `MaxUpgradeLevel`.
- Compila sin errores y las escenas no muestran "Missing Script".
