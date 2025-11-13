using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ShiftingWorldMechanic;

public class ShiftingWorldUI : MonoBehaviour
{
    [Header("Grid (para tiles)")]
    [SerializeField] private GridGenerator grid;

    [Header("Tile Selection (Normal)")]
    [SerializeField] private GameObject tilePanelRoot;
    [SerializeField] private Button[] tileButtons;
    [SerializeField] private TMP_Text[] tileLabels;
    private bool tileChoiceLocked = false;

    [Header("Turret Selection (Otro)")]
    [SerializeField] private GameObject turretPanelRoot;
    [SerializeField] private Button[] turretButtons;
    [SerializeField] private TMP_Text[] turretLabels;

    [Header("Exit Buttons (para tiles)")]
    [SerializeField] private GameObject exitButtonPrefab;
    [SerializeField] private Canvas exitButtonsCanvas;
    private readonly List<Button> exitButtons = new();

    [Header("Torretas (fuente)")]
    [SerializeField] private TurretDatabaseSO turretDatabase;
    [SerializeField] private List<TurretDataSO> turretPool = new();

    [Header("Placement")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayers = ~0;
    //[SerializeField] private float turretYOffset = 0.01f;
    [Tooltip("Si está activo, la UI coloca la torreta. Si está desactivado, delega en TurretPlacer externo.")]
    [SerializeField] private bool placeWithUI = false;

    [Header("Sistema de Dupes")]
    [SerializeField] private bool enableDupesSystem = true;

    // Eventos hacia ShiftingWorldMechanic
    public event Action<World> OnTurretPlacedSuccessfully;
    public event Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;

    // Estado de colocación (independiente)
    private bool tilePlacingMode = false;   // flujo de tiles (Normal)
    private bool turretPlacingMode = false; // flujo de torretas (Otro) solo si placeWithUI = true

    private TileLayout selectedTileLayout;
    private TurretDataSO selectedTurret;

    // Si delegás a un TurretPlacer externo:
    public event Action<TurretDataSO> OnTurretChosen;

    // Callbacks de cierre independientes
    private Action onTileClosed;
    private Action onTurretClosed;

    private ITurretDupeSystem dupeSystem;

    private readonly List<TileLayout> currentTileOptions = new();
    private readonly List<TurretDataSO> currentTurretOptions = new();

    [Header("Barras de progreso (Filled)")]
    [SerializeField] private Image normalWorldFill;   // 0..1
    [SerializeField] private Image otherWorldFill;    // 0..1
    [SerializeField] private Image worldToggleCooldownFill; // 0..1 (recarga)

    [Header("Icono de Mundo")]
    [SerializeField] private Image worldIcon;
    [SerializeField] private Sprite normalWorldSprite;
    [SerializeField] private Sprite otherWorldSprite;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseSpeed = 2.5f; 
    
    [Header("Referencias")]
    [SerializeField] private RectTransform uiContainer;

    private void OnValidate()
    {
        // Auto-cablear cosas típicas si faltan
        if (!grid) grid = FindFirstObjectByType<GridGenerator>();
        if (!cam) cam = Camera.main;
    }


    // --- NUEVO: helper para trackear cada botón ---
    private class ExitBtn
    {
        public string label;
        public Vector3 worldPos;
        public RectTransform rect;
    }

    // Lista con la info de cada botón
    private readonly List<ExitBtn> exitBtnInfo = new();

    // Altura en MUNDO (se multiplica por cellSize)
    [SerializeField] private float buttonWorldYOffset = 2.0f;

    // Altura extra en PANTALLA (pixeles) para Overlay/Camera
    [SerializeField] private float buttonScreenYOffset = 24f;

    // Forzar pivot abajo (0.5, 0) para que el punto quede en la base
    [SerializeField] private bool buttonPivotBottom = true;

    void Awake()
    {
        HideAll(); // arranca limpio
    }

    private void Start()
    {
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>() as ITurretDupeSystem;
        if (TurretDupeUI.Instance != null)
        {
            TurretDupeUI.Instance.ConnectToSystem((TurretDupeSystem)dupeSystem);
        }
    }

    void Update()
    {
        // Colocación de torreta por UI (opcional)
        if (placeWithUI && turretPlacingMode)
            HandleTurretPlacementMode();

        if (tilePlacingMode && exitButtonsCanvas && (cam || Camera.main))
        {
            var camToUse = cam ? cam : Camera.main;

            if (exitButtonsCanvas.renderMode == RenderMode.WorldSpace)
            {
                var toCam = camToUse.transform.position - exitButtonsCanvas.transform.position;
                exitButtonsCanvas.transform.rotation = Quaternion.LookRotation(toCam);
            }

            var canvasRect = exitButtonsCanvas.transform as RectTransform;

            foreach (var e in exitBtnInfo)
            {
                SetExitButtonPosition(
                    e.rect,
                    e.worldPos + Vector3.up * GetWorldYOffset(),
                    exitButtonsCanvas,
                    canvasRect,
                    camToUse
                );
            }
        }

    }

    // ============================
    // API pública para mostrar paneles (ambos pueden estar activos a la vez)
    // ============================
    public void ShowNormalReached(Action closedCb = null)
    {
        tileChoiceLocked = false;
        onTileClosed = closedCb;

        BuildNormalOptions();

        if (tilePanelRoot) tilePanelRoot.SetActive(true);
        
        // ¡NO ocultamos turretPanelRoot!
        HideExitButtons(); // limpio por si venimos de un intento previo
    }

    public void ShowOtherReached(Action closedCb = null)
    {
        onTurretClosed = closedCb;

        BuildOtherOptions();

        if (turretPanelRoot) turretPanelRoot.SetActive(true);
        // ¡NO ocultamos tilePanelRoot!
    }

    // ============================
    // Lógica de selección y colocación de TILE (Normal)
    // ============================
    private void BuildNormalOptions()
    {
        currentTileOptions.Clear();
        if (!grid) return;

        var candidates = grid.GetRandomCandidateSet(3);
        for (int i = 0; i < 3; i++)
        {
            var layout = (i < candidates.Count) ? candidates[i] : null;
            currentTileOptions.Add(layout);

            string label = layout ? layout.name : "N/D";
            int idx = i;
            BindButton(tileButtons, tileLabels, i, label, () => OnChooseTile(idx));
        }
    }

    private void OnChooseTile(int index)
    {
        if (tileChoiceLocked) return;

        var chosen = (index >= 0 && index < currentTileOptions.Count) ? currentTileOptions[index] : null;
        if (!chosen)
        {
            Debug.LogWarning("[ShiftingWorldUI] Opción de tile inválida. Panel sigue abierto.");
            return;
        }

        tileChoiceLocked = true;
        selectedTileLayout = chosen;
        EnterTilePlacementMode();
    }

    private void EnterTilePlacementMode()
    {
        tilePlacingMode = true;

        // Ocultamos solo el panel de selección de tiles (NO tocamos el de torretas)
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        CreateExitButtons();
    }

    private void CreateExitButtons()
    {
        ClearExitButtons();
        exitBtnInfo.Clear();

        var exits = grid.GetAvailableExits();
        if (!exitButtonPrefab || !exitButtonsCanvas) return;

        Camera camToUse = cam ? cam : Camera.main;
        var canvasRect = exitButtonsCanvas.transform as RectTransform;

        foreach (var (label, worldPos) in exits)
        {
            var go = Instantiate(exitButtonPrefab, exitButtonsCanvas.transform);
            var btn = go.GetComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            var txt = go.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt) txt.text = label;

            // Anclaje y pivot para que el punto quede en la base del botón
            if (buttonPivotBottom)
            {
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            exitBtnInfo.Add(new ExitBtn { label = label, worldPos = worldPos, rect = rect });

            // Posición inicial (igual se re-calcula en Update)
            SetExitButtonPosition(
                rect,
                worldPos + Vector3.up * GetWorldYOffset(),
                exitButtonsCanvas,
                canvasRect,
                camToUse
            );

            string captured = label;
            btn.onClick.AddListener(() => OnExitButtonClicked(captured));
        }

        exitButtonsCanvas.gameObject.SetActive(true);
    }


    private void SetExitButtonPosition(
        RectTransform rect,
        Vector3 world,
        Canvas canvas,
        RectTransform canvasRect,
        Camera camToUse)
    {
        switch (canvas.renderMode)
        {
            case RenderMode.WorldSpace:
                // En World Space usamos offset en MUNDO (ya sumado al 'world' que entra)
                rect.position = world;
                break;

            case RenderMode.ScreenSpaceCamera:
                {
                    // Mundo -> Pantalla -> Local del canvas
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camToUse, world);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screen, camToUse, out Vector2 local);
                    // Levantamos en pixeles (pantalla)
                    local.y += buttonScreenYOffset;
                    rect.anchoredPosition = local;
                    break;
                }

            case RenderMode.ScreenSpaceOverlay:
            default:
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camToUse, world);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screen, null, out Vector2 local);
                    local.y += buttonScreenYOffset; // offset en pixeles
                    rect.anchoredPosition = local;
                    break;
                }
        }
    }


    private float GetWorldYOffset()
    {
        // Levantar según cellSize para que se vea bien con distintos tiles
        var layout = grid ? grid.CurrentLayout : null;
        float cs = layout ? layout.cellSize : 1f;
        return buttonWorldYOffset * cs;
    }


private void OnExitButtonClicked(string exitLabel)
    {
        if (!selectedTileLayout) return;

        grid.UI_SetExitByLabel(exitLabel);
        bool ok = grid.AppendNextUsingSelectedExitWithLayout(selectedTileLayout);

        // ⬅️ SIN importar ok, refrescá los botones porque los labels pudieron cambiar
        HideExitButtons();
        CreateExitButtons();

        if (ok)
        {
            Debug.Log($"[ShiftingWorldUI] Tile {selectedTileLayout.name} colocado en exit {exitLabel}");

            PlacementEvents.RaiseTileApplied(new PlacementEvents.TileAppliedInfo
            {
                tileGridCoord = Vector2Int.zero,
                tileId = selectedTileLayout.name,
                expandedGrid = false
            });

            ExitTilePlacementMode();
            CloseTilePanelOnly();
        }
        else
        {
            Debug.LogWarning($"[ShiftingWorldUI] No se pudo colocar el tile: {selectedTileLayout.name}. Probá otro exit.");
            // seguís en modo colocación, pero con botones y labels actualizados
        }
    }


    private int FindExitIndexByLabel(string label)
    {
        var exits = grid.GetAvailableExits();
        for (int i = 0; i < exits.Count; i++)
            if (exits[i].label == label) return i;
        return -1;
    }

    private void ExitTilePlacementMode()
    {
        tilePlacingMode = false;
        selectedTileLayout = null;
        HideExitButtons();
        tileChoiceLocked = false;
    }

    private void CloseTilePanelOnly()
    {
        // cerrar SOLO el flujo de tiles
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        ExitTilePlacementMode();

        onTileClosed?.Invoke();
        onTileClosed = null;

        currentTileOptions.Clear();
        // NO tocar el panel de torretas
    }

    // ============================
    // Lógica de selección y colocación de TORRETA (Otro)
    // ============================
    private void BuildOtherOptions()
    {
        currentTurretOptions.Clear();

        var pool = new List<TurretDataSO>();
        if (turretDatabase && turretDatabase.allTurrets != null)
            foreach (var t in turretDatabase.allTurrets) if (t) pool.Add(t);
        if (turretPool != null)
            foreach (var t in turretPool) if (t) pool.Add(t);

        if (pool.Count == 0)
        {
            Debug.LogWarning("[ShiftingWorldUI] No hay torretas en la DB o turretPool.");
            return;
        }

        Shuffle(pool);

        for (int i = 0; i < 3; i++)
        {
            var so = pool[i % pool.Count];
            currentTurretOptions.Add(so);

            string label = GetTurretDisplayName(so);
            int idx = i;
            BindButton(turretButtons, turretLabels, i, label, () => OnChooseTurret(idx));
        }
    }

    private void OnChooseTurret(int index)
    {
        var so = (index >= 0 && index < currentTurretOptions.Count) ? currentTurretOptions[index] : null;
        if (!so || !so.prefab)
        {
            Debug.LogWarning("[ShiftingWorldUI] Opción de torreta inválida. Panel sigue abierto.");
            return;
        }

        HandleDupeSystem(so);
        selectedTurret = so;

        if (placeWithUI)
        {
            turretPlacingMode = true;
            // Ocultar SOLO el panel de selección de torretas para entrar en modo colocación
            if (turretPanelRoot) turretPanelRoot.SetActive(false);
            Debug.Log($"[ShiftingWorldUI] Elegiste torreta: {so.displayName}. Seleccioná una celda.");
        }
        else
        {
            // Delegado al TurretPlacer externo
            turretPlacingMode = false;
            OnTurretChosen?.Invoke(so);

            // Ocultar el panel de selección de torretas para ver el ghost, pero NO cerrar tiles
            if (turretPanelRoot) turretPanelRoot.SetActive(false);

            Debug.Log($"[ShiftingWorldUI] Torreta seleccionada: {so.displayName}. " +
                      $"Cerrará el panel de torretas al confirmar colocación (NotifyTurretPlaced).");
        }
    }

    private void HandleTurretPlacementMode()
    {
        // Evitar clics sobre UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        // Cancelar: vuelve al panel de selección de torretas (NO cierra todo)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelTurretPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Camera c = cam ? cam : Camera.main;
            if (!c) return;

            Ray ray = c.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
            {
                var slot = hit.collider.GetComponentInParent<CellSlot>();
                if (!slot) return;

                if (!selectedTurret || !selectedTurret.prefab)
                {
                    CancelTurretPlacement();
                    return;
                }

                if (slot.TryPlace(selectedTurret.prefab, selectedTurret))
                {
                    Debug.Log("[ShiftingWorldUI] Torreta colocada.");
                    OnTurretPlacedSuccessfully?.Invoke(World.Otro);

                    EndTurretPlacement();
                    CloseTurretPanelOnly(); // ← cerrar SOLO el panel/flujo de torretas
                }
                else
                {
                    Debug.Log("[ShiftingWorldUI] Celda ocupada. Probá otra.");
                }
            }
        }
    }

    private void EndTurretPlacement()
    {
        turretPlacingMode = false;
        selectedTurret = null;
    }

    private void CancelTurretPlacement()
    {
        Debug.Log("[ShiftingWorldUI] Colocación de torreta cancelada.");
        EndTurretPlacement();
        // Volver a mostrar SOLO el panel de torretas
        if (turretPanelRoot) turretPanelRoot.SetActive(true);
    }

    private void CloseTurretPanelOnly()
    {
        if (turretPanelRoot) turretPanelRoot.SetActive(false);
        EndTurretPlacement();

        onTurretClosed?.Invoke();
        onTurretClosed = null;

        currentTurretOptions.Clear();
        // NO tocar el panel de tiles
    }

    // ============================
    // Notificaciones externas (delegado)
    // ============================
    /// Llamá esto desde tu TurretPlacer cuando la colocación se confirme.
    public void NotifyTurretPlaced(World world)
    {
        OnTurretPlacedSuccessfully?.Invoke(world);
        CloseTurretPanelOnly();
    }

    // ============================
    // Utilidades / Helpers
    // ============================
    private void BindButton(Button[] buttons, TMP_Text[] labels, int index, string label, Action onClick)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return;
        var btn = buttons[index];
        if (!btn) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick());
        btn.interactable = true;
        btn.gameObject.SetActive(true);

        TMP_Text tmp = (labels != null && index < labels.Length) ? labels[index] : null;
        if (tmp == null) tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp) tmp.text = label;

    }

    private void HideAll()
    {
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        if (turretPanelRoot) turretPanelRoot.SetActive(false);
        HideExitButtons();
    }

    private void ClearExitButtons()
    {
        if (!exitButtonsCanvas) return;
        for (int i = exitButtonsCanvas.transform.childCount - 1; i >= 0; i--)
            Destroy(exitButtonsCanvas.transform.GetChild(i).gameObject);

        exitButtons.Clear();   // si aún la usás
        exitBtnInfo.Clear();
    }

    private void HideExitButtons()
    {
        if (exitButtonsCanvas) exitButtonsCanvas.gameObject.SetActive(false);
        ClearExitButtons();
    }


    private string GetTurretDisplayName(TurretDataSO turret)
    {
        if (!enableDupesSystem || dupeSystem == null)
            return !string.IsNullOrEmpty(turret.displayName) ? turret.displayName : turret.name;

        var levelData = dupeSystem.GetTurretLevelData(turret);
        return $"{(!string.IsNullOrEmpty(turret.displayName) ? turret.displayName : turret.name)}";
    }

    private void HandleDupeSystem(TurretDataSO turret)
    {
        if (!enableDupesSystem || dupeSystem == null) return;

        var prev = dupeSystem.GetTurretLevelData(turret);
        int prevLevel = prev.currentLevel;

        dupeSystem.AddDupe(turret);

        var cur = dupeSystem.GetTurretLevelData(turret);
        if (cur.currentLevel > prevLevel)
            OnTurretLevelUp?.Invoke(turret, cur);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var rng = new System.Random();
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void SetWorldProgress(float normal01, float other01)
    {
        if (normalWorldFill)
        {
            if (normalWorldFill.type != Image.Type.Filled) normalWorldFill.type = Image.Type.Filled;
            normalWorldFill.fillAmount = Mathf.Clamp01(normal01);
        }

        if (otherWorldFill)
        {
            if (otherWorldFill.type != Image.Type.Filled) otherWorldFill.type = Image.Type.Filled;
            otherWorldFill.fillAmount = Mathf.Clamp01(other01);
        }
    }

    /// <summary>
    /// Valor normalizado del cooldown: 0 = recién cambié y falta, 1 = listo para cambiar de nuevo.
    /// </summary>
    public void SetWorldToggleCooldown(float normalized)
    {
        if (worldToggleCooldownFill)
        {
            if (worldToggleCooldownFill.type != Image.Type.Filled) worldToggleCooldownFill.type = Image.Type.Filled;
            worldToggleCooldownFill.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    public void SetWorldIcon(ShiftingWorldMechanic.World world)
    {
        if (worldIcon == null) return;

        switch (world)
        {
            case ShiftingWorldMechanic.World.Normal:
                worldIcon.sprite = normalWorldSprite;
                break;
            case ShiftingWorldMechanic.World.Otro:
                worldIcon.sprite = otherWorldSprite;
                break;
        }

        if (uiContainer != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePulse(uiContainer));
        }
    }

    private IEnumerator AnimatePulse(RectTransform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * pulseScale;
        float t = 0f;

        //hacia arriba
        while (t < 1f)
        {
            t += Time.deltaTime / pulseSpeed;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        //hacia abajo
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / pulseSpeed;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }
}
