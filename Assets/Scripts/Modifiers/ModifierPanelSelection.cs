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

    private Action onModifierChosen;
    private List<GameObject> spawnedCards = new List<GameObject>();
    private List<IGameModifier> currentOptions = new List<IGameModifier>();
    public static ModifierPanelSelection Instance { get; private set; }
    void Awake() { Instance = this; }

    public void OpenPanel(Action callback)
    {
        panel.SetActive(true);
        onModifierChosen = callback;

        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        currentOptions = GetRandomModifiers(3);

        for (int i = 0; i < currentOptions.Count; i++)
        {
            var modifier = currentOptions[i];
            int stacks = GameModifiersManager.Instance.GetModifierStacks(modifier);

            GameObject prefabToUse = cardNormalWorldPrefab;
            switch (modifier.Category)
            {
                case ModifierCategory.NormalWorld:
                    prefabToUse = cardNormalWorldPrefab;
                    break;
                case ModifierCategory.OtherWorld:
                    prefabToUse = cardOtherWorldPrefab;
                    break;
                case ModifierCategory.ShiftWorld:
                    prefabToUse = cardShiftWorldPrefab;
                    break;
            }

            GameObject go = Instantiate(prefabToUse, cardContainer);

            int optionIndex = i;

            spawnedCards.Add(go);
        }
    }

    private void OnModifierSelected(int idx)
    {
        GameModifiersManager.Instance.ApplyModifier(currentOptions[idx]);

        onModifierChosen?.Invoke();
    }

    private List<IGameModifier> GetRandomModifiers(int count)
    {
        var pool = new List<IGameModifier>(GameModifiersManager.Instance.allModifiers);
        var selected = new List<IGameModifier>();

        System.Random rng = new System.Random();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = rng.Next(pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return selected;
    }
    public void ShowLegendaryModifier(IGameModifier legendaryModifier)
    {
        panel.SetActive(true);

        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        GameObject prefabToUse = cardNormalWorldPrefab;
        switch (legendaryModifier.Category)
        {
            case ModifierCategory.LegendaryNormal:
                prefabToUse = cardNormalWorldPrefab;
                break;
            case ModifierCategory.LegendaryOther:
                prefabToUse = cardOtherWorldPrefab;
                break;
        }

        GameObject go = Instantiate(prefabToUse, cardContainer);
    }
}
