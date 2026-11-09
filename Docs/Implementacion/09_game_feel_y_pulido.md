# 09 — Game feel, pulido y riesgos técnicos

Depende de los MD 01–08. Implementar **por bloques en este orden (A → F)**, con un commit por bloque y una pausa para probar entre cada uno. Los bloques F son opcionales y se hacen solo si te lo pido.

## Reglas generales para este MD

- Todo el feedback visual usa **tiempo unscaled** cuando tiene que verse con el juego pausado o acelerado (paneles, cards, popups de UI). Lo que pasa en el mundo usa tiempo escalado.
- Todo lo que se instancia seguido (VFX, orbes, textos) va **con pool**: `VfxPool` / `PooledVfx` (`Assets/Scripts/Core/`) o un pool nuevo con la misma idea.
- Valores en `[SerializeField]`, sin hardcodear. Los "juice" globales (intensidad del shake, cámara lenta, flash) se centralizan en un ScriptableObject `FeedbackSettings` (`Assets/Scripts/Feedback/FeedbackSettings.cs`) para poder ajustarlos todos desde un solo asset.
- Crear la carpeta `Assets/Scripts/Feedback/` para las clases nuevas de este MD.

---

## A. Riesgos técnicos (primero)

1. **Cola de paneles modales**: `Feedback/ModalPanelQueue.cs` (singleton en GameScene).
   - Hoy pueden abrirse al mismo tiempo el panel de tiles (`ShiftingWorldUI.ShowNormalReached`), el de torretas (`ShowOtherReached`), el de modificadores (`ModifierWaveTrigger`, MD 04) y el extra pick de una zona (MD 06).
   - API: `Enqueue(Action open, Func<bool> isOpen)`. Muestra un panel a la vez y abre el siguiente cuando `isOpen()` pasa a `false`.
   - Prioridad: modificador > extra pick > tiles / torretas.
   - Mientras hay un panel de modificador abierto, las barras de modo no disparan su panel: quedan en 100 esperando, igual que hoy con `normalWaitingChoice`.
2. **Logs en código que corre mucho**:
   - `TurretDupeUI` loguea en cada barra y en cada duplicado. `ShiftingWorldMechanic` loguea en cada cambio.
   - Crear `Core/Log.cs` con métodos `[System.Diagnostics.Conditional("UNITY_EDITOR")]` (`Log.Info`, `Log.Warn`) que mantengan el formato `[ClassName] mensaje`, y reemplazar los `Debug.Log` de Update, de los loops y de los eventos frecuentes.
   - Los `LogWarning` / `LogError` de configuración se quedan como están.
3. **Tutorial**: en `TutorialManager` / `TutorialStep`, agregar pasos para:
   - las esencias por kill (el color según el modo);
   - la niebla y el zoom;
   - las zonas de recompensa;
   - el panel de modificadores;
   - los duplicados de tiles.

   Usar los eventos que ya existen en `TutorialEventHub` y agregar los que falten (`RaiseModifierChosen`, `RaiseRewardClaimed`, `RaiseZoomedOut`). Pasame los textos de cada paso para revisarlos antes del commit.
4. **No migrar a Input System** en este MD.

## B. Feedback de economía y decisiones

5. **Orbes de esencia**: `Feedback/EssenceOrbFX.cs`.
   - En cada kill que da esencias (`WorldSwitchPoints`, MD 01), sale un orbe (Image de UI con pool) desde la posición en pantalla del enemigo hasta el contador del HUD, por una curva bezier con un punto de control arriba. Duración 0.5–0.7 s, con ease-in.
   - Azul o rojo según el modo, más un ícono distinto (gota / llama) para no depender solo del color.
   - Al llegar, el contador hace un punch de escala de 1.15 en 0.1 s y recién ahí suma el número.
   - Máximo de orbes simultáneos (por ejemplo 20). Si se pasa, se agrupan en uno con más valor.
   - Reemplaza al popup de texto de `EssencePopupUI`. El contador se queda.
6. **Chime de kill en cadena**: SFX de esencia con pitch que sube +0.05 por kill dentro de una ventana de 1 s, con tope de +0.5. Se resetea al cortarse la cadena.
7. **Badge "LEVEL UP!" y largo de camino**:
   - El badge ya está en el MD 08.
   - En `TileOptionButton`, agregar `"+N cells"` con lo que suma el layout al camino más largo. `IRunnerService.TryGetRouteLength` existe en `Tiles/Services/RunnerService.cs`. Si calcularlo para cada opción es caro, usar la cantidad de celdas `Path` del layout y avisame.
8. **Aviso de modificador**: en el HUD de oleadas (`UIController`), `"Modifier in N waves"`. Cuando falta 1 oleada, el texto pulsa.

## C. Combate

9. **Hit flash**: `Feedback/HitFlash.cs` en los prefabs de enemigos.
   - Escucha `EnemyHealth.OnDamaged` y pone `_EmissionColor` (o el color del shader que usen) en blanco por 0.06 s con `MaterialPropertyBlock`.
   - Punch de escala de 1.08 en 0.08 s.
   - No se dispara más de una vez cada 0.05 s.
10. **Muerte**: al `OnDied`, spawnear un VFX de muerte por zona (goblin, esqueleto, vampiro) con `VfxPool`, en lugar de que el enemigo desaparezca sin más. Si hay un shader de disolve, usarlo 0.25 s antes de devolver al pool.
11. **Torretas**:
    - Retroceso del cañón (un `Transform barrel` opcional, `-0.1` en z local y vuelta en 0.08 s) y muzzle flash con pool en cada disparo. Engancharlo donde dispara cada variante (`BasicProjectileShooter`, `BasicRaycastShooter`, `FireTurret`, `TeslaTurret`).
    - Sniper: línea de trazado de 0.05 s.
    - Tesla: `LineRenderer` con ruido entre los objetivos de la cadena.
12. **`DamageTextPopup`**: con pool, máximo de textos simultáneos. Los golpes que pasan del 30% de la vida máxima se muestran más grandes y en otro color.
13. **Boss**:
    - Al spawnear un `EnemyType` Boss/Boss2/Boss3: banner `"BOSS: <enemyName>"` 2 s, pan de cámara suave al spawn y vuelta.
    - Barra de vida grande arriba de la pantalla, conectada a `EnemyHealth.OnDamaged`.
    - Al morir: cámara lenta de 0.3 s a 0.3x (con `GameTime`, liberándola después) y shake fuerte.

## D. Core y cámara

14. **Golpe al core**: en `Core.TakeDamage`:
    - viñeta roja de 0.3 s (overlay de UI o Volume de post-proceso);
    - shake corto;
    - pulso en el texto de vida del HUD (`UIController.UpdateCoreHealth`).

    Si la vida queda en 3 o menos, la viñeta queda tenue y fija.
15. **`Feedback/CameraShake.cs`** en la cámara de GameScene: shake por trauma (`AddTrauma(0.0–1.0)`, decae con el tiempo, offset con Perlin). Se aplica como offset local para no pelearse con el movimiento de `CameraController`. Intensidad global desde `FeedbackSettings` y desde la opción del bloque F.

## E. Features nuevas

16. **Disolve de la niebla** (MD 05): en vez de desactivar el fog tile en el acto, animar un fade con ruido de 0.4 s que sale desde el lado más cercano al tile colocado (propiedad `_Dissolve` en el shader de niebla), y recién después devolverlo al pool.
17. **Zona de recompensa** (MD 06):
    - Borde con pulso de alpha constante.
    - Al reclamarla: haz de luz vertical de 0.6 s, burst de partículas del color de la recompensa, toast con lo ganado y un SFX distinto por `RewardType`.
18. **Level up por duplicados**: al `OnTurretLevelUp` / `OnTileLevelUp`:
    - todas las instancias de ese tipo o layout hacen un flash al mismo tiempo;
    - aparece un `"Lv X"` flotante sobre cada una;
    - SFX con un pitch que sube según el nivel.
19. **Cambio de mundo**: `WorldShiftPulse` ya existe. Sumar:
    - cámara lenta de 0.15 s a 0.3x;
    - aberración cromática o distorsión de 0.2 s (Volume de URP, si hay post-proceso);
    - crossfade de música de 0.5 s con low-pass en `ShiftingWorldAudio`.

    Si apretás `J` durante el cooldown: el anillo de cooldown hace shake y suena un SFX de "no disponible".
20. **Cards de modificadores**: entrada escalonada (0.08 s entre cards, scale 0.8 → 1 y alpha), hover a 1.05 con un brillo del color del modo, y al confirmar la card elegida crece mientras las otras se desvanecen. Todo con tiempo unscaled.
21. **Colocación de tiles**: `TilePlacementAnimator` / `TilePlacementSequencer` ya existen. Asegurar que cada celda caiga con un pequeño rebote y polvo, escalonada desde la entrada hacia las salidas.

## F. Diseño, rejugabilidad y opciones (opcional, solo si te lo pido)

22. **Llamar la oleada antes**: si `WaveManager.ForceStartNextWave()` se usa mientras corre `NextWaveCountdownRoutine`, dar esencias extra del modo activo, proporcionales al tiempo restante (por ejemplo 1 por segundo restante, configurable). Mostrarlo en el botón "Start Wave": `"+N"`.
23. **Resumen de partida** en `ResultSceneController`:
    - modificadores elegidos (íconos);
    - nivel máximo de cada torreta y de cada tile;
    - largo del camino;
    - fog tiles revelados;
    - zonas reclamadas;
    - kills por modo.

    `GameManager` guarda esos datos en un `RunStats` antes de cargar `ResultScene`. Agregar un **puntaje** = oleadas × 100 + kills + zonas × 50 + bonus por boss (pendiente de balance).
24. **Atajos**: 1/2/3 para la velocidad (`GameSpeedController` / `SpeedButtonGroup`) y Tab mantenido para mostrar el rango de todas las torretas (`TurretRangeVisualizer`). Fuera del modo colocación de tiles, donde Tab ya se usa (MD 08).
25. **Opciones** (`OptionsMenu`): toggle de camera shake (0 / 50 / 100%), toggle de cámara lenta y opción de reducir flashes. Guardar en `PlayerPrefs`.
26. **Empezar desde una zona**: después de vencer al boss de la zona 1 o 2 por primera vez, el menú permite empezar en la oleada 16 o 31. A cambio, se ganan 50% menos esencias en esa partida. Usar `WaveManager.JumpToWave`. **Pendiente de decisión:** no implementar sin mi confirmación.

## Criterios de aceptación

- **A**: nunca hay dos paneles abiertos a la vez. Un build de desarrollo no muestra los logs de código frecuente. El tutorial cubre las mecánicas nuevas.
- **B**: cada kill manda un orbe del color o ícono del modo al contador, y el número sube cuando llega. El aviso de modificador aparece en el HUD.
- **C**: cada golpe se nota (flash y punch). Las muertes tienen VFX. Los bosses tienen banner y barra.
- **D**: un golpe al core tiene viñeta y shake.
- **E**: la niebla se disuelve, las zonas tienen haz al reclamarse, los level ups hacen flash en todas las instancias y el cambio de mundo tiene cámara lenta y crossfade.
- Sin caídas de FPS con la oleada más densa a velocidad x3 (medir con el Profiler y pasarme el resultado).
