# 06 — Zonas de recompensa (borde amarillo)

GDD: 2.5.3 Reward zones. Depende de los MD 05 (niebla, zoom y `OnTilePlaced`), 04 (modificadores) y 01 (esencias).

## Objetivo

- En la niebla hay zonas de recompensa marcadas con un **borde amarillo**.
- El borde se ve a través de la niebla **solo con zoom out** (`ZoomNormalized` ≥ un umbral). Con el modificador N12 (Pathfinder), se ve siempre.
- Cuando el camino llega a la zona (un tile colocado se superpone con ella), el jugador recibe la recompensa y la zona se consume.
- Una zona nunca bloquea ni condiciona la colocación de tiles. El camino nunca se vuelve imposible.

## Cambios

1. **`Assets/Scripts/Rewards/RewardType.cs`**: `enum RewardType { BlueEssence, RedEssence, FreeTurretDupe, FreeTileDupe, ExtraModifierPick }`.
2. **`Assets/Scripts/Rewards/RewardTableSO.cs`** (`CreateAssetMenu "Rewards/Reward Table"`): lista de entradas con `RewardType type`, `int amount` (para esencias) y `float weight`. Valores iniciales:
   - BlueEssence 25, peso 3
   - RedEssence 25, peso 3
   - FreeTurretDupe, peso 2
   - FreeTileDupe, peso 2
   - ExtraModifierPick, peso 1
3. **`Assets/Scripts/Rewards/RewardZone.cs`** (MonoBehaviour en el prefab de la zona):
   - `Bounds` en mundo del tamaño de un fog tile (`GridGenerator.TileFootprint`).
   - Borde con un `LineRenderer` en loop (4 esquinas), material unlit amarillo emisivo, dibujado **por encima** de la niebla (`heightOffset` mayor que el de los fog tiles). Opcional: un pulso suave de alpha.
   - `RewardType` asignado al spawnear.
   - Ícono del tipo de recompensa sobre la zona, visible con la misma regla que el borde.
   - `SetVisible(bool)`.
4. **`Assets/Scripts/Rewards/RewardZoneSystem.cs`** (MonoBehaviour en GameScene):
   - Refs: `GridGenerator`, `FogOfWarSystem`, `CameraController`, `RewardTableSO` y el prefab `RewardZone`.
   - Config: `initialZones` (3), `minDistanceTiles` (2), `maxDistanceTiles` (6) desde el tile inicial, `respawnEveryNWaves` (5), `maxActiveZones` (3) y `visibleZoomThreshold` (0.6).
   - **Spawn**: elegir posiciones de la grilla de fog tiles (misma alineación que `FogOfWarSystem`) que estén todavía con niebla, dentro del rango de distancia, sin superponerse con tiles colocados ni con otra zona. Al inicio spawnea `initialZones`. Cada `respawnEveryNWaves` (con `WaveManager.OnWaveEnded`) agrega una nueva si hay menos que `maxActiveZones`, siempre a una distancia mayor que la del tile colocado más lejano.
   - **Visibilidad**: en `CameraController.OnZoomChanged`, `zone.SetVisible(zoom >= visibleZoomThreshold || GetStat(RewardZoneAlwaysVisible) > 0)`.
   - **Claim**: en `GridGenerator.OnTilePlaced`, para cada zona activa, si el AABB del tile nuevo se superpone con `zone.Bounds`, se entrega la recompensa, se reproduce un VFX/SFX y se devuelve la zona al pool.
5. **Entrega de recompensas** (`RewardZoneSystem.Grant(RewardType, int amount)`):
   - Multiplicador `GameModifiersManager.Instance.GetMultiplier(ModifierStat.RewardZoneBonus)` (N12 = +50%) sobre las esencias.
   - `BlueEssence` / `RedEssence`: `WorldSwitchPoints.AddBlueEssence` / `AddRedEssence`, y actualizar `EssencePopupUI`.
   - `FreeTurretDupe`: un tipo de torreta ya colocado al azar → `TurretDupeSystem.AddDupe(data)`. Si no hay torretas colocadas, se cambia por `RedEssence`.
   - `FreeTileDupe`: un `TileLayout` ya colocado al azar (excluyendo `initialLayout`) → `TileDupeSystem.AddDupe(layout)`. Si no hay ninguno, se cambia por `BlueEssence`.
   - `ExtraModifierPick`: `ModifierPanelSelection.OpenExtraPick()` (MD 04) con pausa, usando el pool del modo activo.
   - Mostrar un texto flotante o toast con lo recibido: `"+25 Normal Essences"`, `"Free copy: Fire Turret"`, etc.
6. **Editor (checklist para mí)**: prefab `RewardZone` con `LineRenderer`, material amarillo y el asset `RewardTable`. `RewardZoneSystem` en GameScene con sus refs.

## Criterios de aceptación

- Al empezar hay 3 zonas en la niebla. Con zoom in no se ven; con zoom out se ven sus bordes amarillos.
- Al construir el camino hasta una zona, se recibe la recompensa y la zona desaparece.
- Ninguna zona aparece sobre un tile ya colocado.
- La generación de opciones de tiles (`GetRandomCandidateSet`) no cambia con o sin zonas.
- Con N12 activo, las zonas se ven con cualquier zoom y las recompensas de esencia valen un 50% más.
- Cada 5 oleadas aparece una zona nueva si hay menos de 3.
