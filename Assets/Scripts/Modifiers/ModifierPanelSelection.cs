using System;
using System.Collections.Generic;
using UnityEngine;

public class ModifierPanelSelection : MonoBehaviour
{
    public GameObject panel;
    public Transform cardContainer;

    public GameObject cardNormalWorldPrefab;
    public GameObject cardOtherWorldPrefab;
    public GameObject cardShiftWorldPrefab;

    [Header("Extra Pick")]
    [SerializeField, Min(1)] private int extraPickOptions = 3;

    [Header("Confirm Animation (unscaled)")]
    [SerializeField, Min(0f)] private float confirmDuration = 0.3f;

    private readonly List<ModifierCardUI> spawnedCardUIs = new List<ModifierCardUI>();
    private bool confirming;

    private Action onModifierChosen;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();
    private readonly List<ModifierSO> currentOptions = new List<ModifierSO>();
    private bool extraPickPaused;

    public static ModifierPanelSelection Instance { get; private set; }
    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    private void Start()
    {
        if (GameModifiersManager.Instance != null)
            GameModifiersManager.Instance.InjectModifierPanelSelection(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (extraPickPaused)
        {
            extraPickPaused = false;
            GameTime.ReleasePause();
        }
    }

    public void OpenPanel(List<ModifierSO> options, Action onChosen)
    {
        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("[ModifierPanelSelection] No modifier options to show");
            onChosen?.Invoke();
            return;
        }

        if (panel) panel.SetActive(true);
        onModifierChosen = onChosen;

        ClearCards();
        currentOptions.Clear();
        currentOptions.AddRange(options);

        for (int i = 0; i < currentOptions.Count; i++)
        {
            var modifier = currentOptions[i];
            GameObject prefabToUse = GetPrefabFor(modifier.Category);
            if (prefabToUse == null)
            {
                Debug.LogWarning("[ModifierPanelSelection] No card prefab assigned for " + modifier.Category);
                continue;
            }

            GameObject go = Instantiate(prefabToUse, cardContainer);
            spawnedCards.Add(go);

            int optionIndex = i;
            var card = go.GetComponent<ModifierCardUI>();
            if (card == null) card = go.AddComponent<ModifierCardUI>();
            card.Bind(modifier, () => OnModifierSelected(optionIndex), spawnedCardUIs.Count);
            spawnedCardUIs.Add(card);
        }
    }

    public void OpenExtraPick(Action onChosen = null)
    {
        if (GameModifiersManager.Instance == null)
        {
            onChosen?.Invoke();
            return;
        }

        var category = GameModifiersManager.CategoryForCurrentWorld();
        var options = GameModifiersManager.Instance.GetRandomAvailable(category, extraPickOptions);
        if (options.Count == 0)
        {
            Debug.Log("[ModifierPanelSelection] No modifiers left for an extra pick");
            onChosen?.Invoke();
            return;
        }

        GameTime.RequestPause();
        extraPickPaused = true;

        OpenPanel(options, () =>
        {
            if (extraPickPaused)
            {
                extraPickPaused = false;
                GameTime.ReleasePause();
            }
            onChosen?.Invoke();
        });
    }

    private GameObject GetPrefabFor(ModifierCategory category)
    {
        switch (category)
        {
            case ModifierCategory.OtherWorld:
            case ModifierCategory.LegendaryOther:
                return cardOtherWorldPrefab != null ? cardOtherWorldPrefab : cardNormalWorldPrefab;
            case ModifierCategory.ShiftWorld:
                return cardShiftWorldPrefab != null ? cardShiftWorldPrefab : cardNormalWorldPrefab;
            default:
                return cardNormalWorldPrefab;
        }
    }

    private void OnModifierSelected(int idx)
    {
        if (idx < 0 || idx >= currentOptions.Count || confirming) return;

        if (confirmDuration > 0f && isActiveAndEnabled)
        {
            StartCoroutine(ConfirmRoutine(idx));
            return;
        }

        ApplySelection(idx);
    }

    private System.Collections.IEnumerator ConfirmRoutine(int idx)
    {
        confirming = true;
        for (int i = 0; i < spawnedCardUIs.Count; i++)
        {
            var card = spawnedCardUIs[i];
            if (card != null) StartCoroutine(card.PlayConfirm(i == idx, confirmDuration));
        }

        float t = 0f;
        while (t < confirmDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        confirming = false;
        ApplySelection(idx);
    }

    private void ApplySelection(int idx)
    {
        if (idx < 0 || idx >= currentOptions.Count) return;

        var chosen = currentOptions[idx];
        if (GameModifiersManager.Instance != null)
            GameModifiersManager.Instance.ApplyModifier(chosen);

        ClosePanel();
        TutorialEventHub.RaiseModifierChosen();

        var callback = onModifierChosen;
        onModifierChosen = null;
        callback?.Invoke();
    }

    private void ClosePanel()
    {
        ClearCards();
        currentOptions.Clear();
        if (panel) panel.SetActive(false);
    }

    private void ClearCards()
    {
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card);
        spawnedCards.Clear();
        spawnedCardUIs.Clear();
    }
}
