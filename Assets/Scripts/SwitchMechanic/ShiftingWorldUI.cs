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

    [Header("Turret Selection (Other)")]
    [SerializeField] private GameObject turretPanelRoot;
    [SerializeField] private Button[] turretButtons;
    [SerializeField] private TMP_Text[] turretLabels;

    [Header("Exit Buttons (para tiles)")]
    [SerializeField] private GameObject exitButtonPrefab;
    [SerializeField] private Canvas exitButtonsCanvas;
    private readonly List<Button> exitButtons = new();

    [Header("Turrets (source)")]
    [SerializeField] private TurretDatabaseSO turretDatabase;
    [SerializeField] private List<TurretDataSO> turretPool = new();

    [Header("Placement")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayers = ~0;
    [Tooltip("If enabled, the UI places the turret. Otherwise placement is delegated to an external TurretPlacer.")]
    [SerializeField] private bool placeWithUI = false;

    [Header("Dupe System")]
    [SerializeField] private bool enableDupesSystem = true;

    public event Action<World> OnTurretPlacedSuccessfully;
    public event Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;

    private bool tilePlacingMode = false;
    private bool turretPlacingMode = false;

    private TileLayout selectedTileLayout;
    private TurretDataSO selectedTurret;

    public event Action<TurretDataSO> OnTurretChosen;

    private Action onTileClosed;
    private Action onTurretClosed;

    private ITurretDupeSystem dupeSystem;

    private readonly List<TileLayout> currentTileOptions = new();
    private readonly List<TurretDataSO> currentTurretOptions = new();

    [Header("Progress Bars (Filled)")]
    [SerializeField] private Image normalWorldFill;
    [SerializeField] private Image otherWorldFill;
    [SerializeField] private Image worldToggleCooldownFill;

    [Header("Meter Shader Value (_MeterValue)")]
    [Tooltip("Graphic whose material uses the shader _MeterValue parameter.")]
    [SerializeField] private Graphic meterGraphic;
    [SerializeField, Range(0f, 1f)]
    private float meterValue = 0f;

    private Material meterMaterial;
    private static readonly int MeterValuePropertyId = Shader.PropertyToID("_MeterValue");

    [Header("World Icon")]
    [SerializeField] private Image worldIcon;
    [SerializeField] private Sprite normalWorldSprite;
    [SerializeField] private Sprite otherWorldSprite;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("References")]
    [SerializeField] private RectTransform uiContainer;

    private Coroutine blinkRoutine;
    [SerializeField] private float blinkSpeed = 0.6f;
    [SerializeField] private Color blinkColor = Color.white * 1.5f;

    private ShiftingWorldMechanic.World currentWorld;

    private void OnValidate()
    {
        if (!grid) grid = FindFirstObjectByType<GridGenerator>();
        if (!cam) cam = Camera.main;

        TryInitMeterMaterial();
        ApplyMeterValueToMaterial();
    }

    private void TryInitMeterMaterial()
    {
        if (meterGraphic == null) return;

        if (meterMaterial == null)
        {
            meterMaterial = meterGraphic.material;
        }
    }

    private void ApplyMeterValueToMaterial()
    {
        if (meterMaterial == null) return;

        if (meterMaterial.HasFloat(MeterValuePropertyId))
        {
            meterMaterial.SetFloat(MeterValuePropertyId, Mathf.Clamp01(meterValue));
        }
    }

    private void SetMeterValue(float normalized)
    {
        meterValue = Mathf.Clamp01(normalized);
        TryInitMeterMaterial();
        ApplyMeterValueToMaterial();
    }

    private class ExitBtn
    {
        public string label;
        public Vector3 worldPos;
        public RectTransform rect;
    }

    private readonly List<ExitBtn> exitBtnInfo = new();

    [SerializeField] private float buttonWorldYOffset = 2.0f;

    [SerializeField] private float buttonScreenYOffset = 24f;

    [SerializeField] private bool buttonPivotBottom = true;

    void Awake()
    {
        HideAll();
    }

    private void OnEnable()
    {
        EscapeRouter.Register(HandleEscape, EscapeRouter.PriorityPlacement);
    }

    private void OnDisable()
    {
        EscapeRouter.Unregister(HandleEscape);
    }

    private bool HandleEscape()
    {
        if (tilePlacingMode)
        {
            CancelTilePlacement();
            return true;
        }

        if (!placeWithUI || !turretPlacingMode) return false;
        CancelTurretPlacement();
        return true;
    }

    private void Start()
    {
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>() as ITurretDupeSystem;
        if (TurretDupeUI.Instance != null)
        {
            TurretDupeUI.Instance.ConnectToSystem((TurretDupeSystem)dupeSystem);
        }

        TryInitMeterMaterial();
        ApplyMeterValueToMaterial();
    }

    void Update()
    {
        if (placeWithUI && turretPlacingMode)
            HandleTurretPlacementMode();

        if (tilePlacingMode && !GameTime.IsPaused && Input.GetMouseButtonDown(1))
            CancelTilePlacement();

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

    public void ShowNormalReached(Action closedCb = null)
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowPlaceTiles)
            return;

        tileChoiceLocked = false;
        onTileClosed = closedCb;

        BuildNormalOptions();

        if (!HasAnyTileOption())
        {
            Debug.LogWarning("[ShiftingWorldUI] No tile fits any open exit. The tile reward is skipped");
            CloseTilePanelOnly();
            return;
        }

        if (tilePanelRoot) tilePanelRoot.SetActive(true);

        HideExitButtons();
    }

    public void ShowOtherReached(Action closedCb = null)
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.AllowPlaceTurrets)
            return;

        onTurretClosed = closedCb;

        BuildOtherOptions();

        if (turretPanelRoot) turretPanelRoot.SetActive(true);
    }

    private void BuildNormalOptions()
    {
        currentTileOptions.Clear();
        if (!grid) return;

        var candidates = grid.GetRandomCandidateSet(3);
        for (int i = 0; i < 3; i++)
        {
            var layout = (i < candidates.Count) ? candidates[i] : null;
            currentTileOptions.Add(layout);

            if (layout == null)
            {
                if (tileButtons != null && i < tileButtons.Length && tileButtons[i])
                    tileButtons[i].gameObject.SetActive(false);
                continue;
            }

            int idx = i;
            BindButton(tileButtons, tileLabels, i, layout.name, () => OnChooseTile(idx));
        }
    }

    private bool HasAnyTileOption()
    {
        for (int i = 0; i < currentTileOptions.Count; i++)
            if (currentTileOptions[i] != null) return true;
        return false;
    }

    private void CancelTilePlacement()
    {
        Debug.Log("[ShiftingWorldUI] Tile placement cancelled");
        tilePlacingMode = false;
        selectedTileLayout = null;
        tileChoiceLocked = false;
        HideExitButtons();

        if (tilePanelRoot) tilePanelRoot.SetActive(true);
    }

    private void OnChooseTile(int index)
    {
        if (tileChoiceLocked) return;

        var chosen = (index >= 0 && index < currentTileOptions.Count) ? currentTileOptions[index] : null;
        if (!chosen)
        {
            Debug.LogWarning("[ShiftingWorldUI] Invalid tile option. The panel stays open");
            return;
        }

        tileChoiceLocked = true;
        selectedTileLayout = chosen;
        EnterTilePlacementMode();
    }

    private void EnterTilePlacementMode()
    {
        tilePlacingMode = true;

        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        CreateExitButtons();
    }

    private void CreateExitButtons()
    {
        ClearExitButtons();
        exitBtnInfo.Clear();

        var exits = grid.GetAvailableExitsFor(selectedTileLayout);
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

            if (buttonPivotBottom)
            {
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            exitBtnInfo.Add(new ExitBtn { label = label, worldPos = worldPos, rect = rect });

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
                rect.position = world;
                break;

            case RenderMode.ScreenSpaceCamera:
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camToUse, world);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screen, camToUse, out Vector2 local);
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
                    local.y += buttonScreenYOffset;
                    rect.anchoredPosition = local;
                    break;
                }
        }
    }

    private float GetWorldYOffset()
    {
        var layout = grid ? grid.CurrentLayout : null;
        float cs = layout ? layout.cellSize : 1f;
        return buttonWorldYOffset * cs;
    }

    private void OnExitButtonClicked(string exitLabel)
    {
        if (!selectedTileLayout) return;

        grid.UI_SetExitByLabel(exitLabel);
        bool ok = grid.AppendNextUsingSelectedExitWithLayout(selectedTileLayout);

        HideExitButtons();
        CreateExitButtons();

        if (ok)
        {
            Debug.Log($"[ShiftingWorldUI] Tile {selectedTileLayout.name} placed on exit {exitLabel}");

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
            Debug.LogWarning($"[ShiftingWorldUI] Could not place tile {selectedTileLayout.name}. Try another exit");
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
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        ExitTilePlacementMode();

        onTileClosed?.Invoke();
        onTileClosed = null;

        currentTileOptions.Clear();
    }

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
            Debug.LogWarning("[ShiftingWorldUI] No turrets in the database or turretPool");
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
            Debug.LogWarning("[ShiftingWorldUI] Invalid turret option. The panel stays open");
            return;
        }

        HandleDupeSystem(so);
        selectedTurret = so;

        if (placeWithUI)
        {
            StartCoroutine(DelayedTurretSelectFlow(selectedTurret));
        }
        else
        {
            turretPlacingMode = false;
            OnTurretChosen?.Invoke(so);

            StartCoroutine(DelayedTurretSelectFlow(selectedTurret));

            Debug.Log($"[ShiftingWorldUI] Turret selected: {so.displayName}. " +
                      "The turret panel closes once placement is confirmed (NotifyTurretPlaced)");
        }
    }

    private IEnumerator DelayedTurretSelectFlow(TurretDataSO selectedTurret)
    {
        turretPlacingMode = true;

        yield return new WaitForSeconds(1.5f);

        if (turretPanelRoot)
            turretPanelRoot.SetActive(false);

        Debug.Log($"[ShiftingWorldUI] Turret selected: {selectedTurret.displayName}. Select a cell");
    }

    private void HandleTurretPlacementMode()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(1))
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
                    Debug.Log("[ShiftingWorldUI] Turret placed");
                    OnTurretPlacedSuccessfully?.Invoke(World.Other);

                    EndTurretPlacement();
                    StartCoroutine(CloseTurretPanelDelayed());
                }
                else
                {
                    Debug.Log("[ShiftingWorldUI] Cell is occupied. Try another one");
                }
            }
        }
    }

    private IEnumerator CloseTurretPanelDelayed(float delay = 1.5f)
    {
        yield return new WaitForSeconds(delay);

        CloseTurretPanelOnly();
    }

    private void EndTurretPlacement()
    {
        turretPlacingMode = false;
        selectedTurret = null;
    }

    private void CancelTurretPlacement()
    {
        Debug.Log("[ShiftingWorldUI] Turret placement cancelled");
        EndTurretPlacement();
        if (turretPanelRoot) turretPanelRoot.SetActive(true);
    }

    private void CloseTurretPanelOnly()
    {
        if (turretPanelRoot) turretPanelRoot.SetActive(false);
        EndTurretPlacement();

        onTurretClosed?.Invoke();
        onTurretClosed = null;

        currentTurretOptions.Clear();
    }

    public void NotifyTurretPlaced(World world)
    {
        OnTurretPlacedSuccessfully?.Invoke(world);
        CloseTurretPanelOnly();
    }

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

        exitButtons.Clear();
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

    public void SetWorldToggleCooldown(float normalized)
    {
        float clamped = Mathf.Clamp01(normalized);

        if (worldToggleCooldownFill)
        {
            if (worldToggleCooldownFill.type != Image.Type.Filled)
                worldToggleCooldownFill.type = Image.Type.Filled;

            worldToggleCooldownFill.fillAmount = clamped;
        }

        SetMeterValue(clamped);

        if (clamped >= 1f)
        {
            if (blinkRoutine == null)
                blinkRoutine = StartCoroutine(BlinkWorldIcon());
        }
        else
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
                ResetWorldIconColor();
            }
        }
    }
    private Color GetCurrentWorldBaseColor()
    {
        return currentWorld == ShiftingWorldMechanic.World.Normal
            ? Color.blue
            : Color.red;
    }

    private void ResetWorldIconColor()
    {
        if (worldIcon == null) return;
        worldIcon.color = GetCurrentWorldBaseColor();
    }

    private IEnumerator BlinkWorldIcon()
    {
        if (worldIcon == null)
            yield break;

        bool toggle = false;

        while (true)
        {
            toggle = !toggle;

            worldIcon.color = toggle ? blinkColor : GetCurrentWorldBaseColor();

            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    public void SetWorldIcon(ShiftingWorldMechanic.World world)
    {
        if (worldIcon == null) return;

        currentWorld = world;

        switch (world)
        {
            case ShiftingWorldMechanic.World.Normal:
                worldIcon.sprite = normalWorldSprite;
                worldIcon.color = Color.blue;
                break;

            case ShiftingWorldMechanic.World.Other:
                worldIcon.sprite = otherWorldSprite;
                worldIcon.color = Color.red;
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

        while (t < 1f)
        {
            t += Time.deltaTime / pulseSpeed;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / pulseSpeed;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    public void ForceOpenTilePanel()
    {
        tileChoiceLocked = false;
        onTileClosed = null;
        BuildNormalOptions();
        if (tilePanelRoot) tilePanelRoot.SetActive(true);
    }
}
