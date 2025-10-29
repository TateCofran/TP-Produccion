using System;
using System.Collections.Generic;
using UnityEngine;

public class ModifierPanelSelection : MonoBehaviour
{
    public GameObject panel;
    public Transform cardContainer;

    // Referencias a los prefabs de cartas de cada mundo
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

        // Limpiar cards previas
        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        // Elegir 3 modificadores random (sin repetir)
        currentOptions = GetRandomModifiers(3);

        // Instanciar cards y setear info
        for (int i = 0; i < currentOptions.Count; i++)
        {
            var modifier = currentOptions[i];
            int stacks = GameModifiersManager.Instance.GetModifierStacks(modifier);

            // ---- ELEGIR PREFAB SEGÚN EL MUNDO DEL MODIFICADOR ----
            GameObject prefabToUse = cardNormalWorldPrefab;
            switch (modifier.Category) // <--- asegurate que cada IGameModifier tenga la propiedad Category
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
            //var cardUI = go.GetComponent<ModifierCardUI>();
            int optionIndex = i;
           // cardUI.Setup(modifier, () => OnModifierSelected(optionIndex), stacks);
            spawnedCards.Add(go);
        }
    }

    private void OnModifierSelected(int idx)
    {
        //bool addLegendary = GameModifiersManager.Instance.WillAddLegendaryAfter(modifier: currentOptions[idx]);
        GameModifiersManager.Instance.ApplyModifier(currentOptions[idx]);

        // SOLO ocultá el panel si NO se va a mostrar legendario
        //if (!addLegendary)
           // panel.SetActive(false);

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
        // Mostrá el panel si está oculto, o podés crear un panel aparte
        panel.SetActive(true);

        // Limpiar cards previas (opcional si usás un panel aparte)
        foreach (var card in spawnedCards)
            Destroy(card);
        spawnedCards.Clear();

        // Determinar el prefab correcto
        GameObject prefabToUse = cardNormalWorldPrefab;
        switch (legendaryModifier.Category)
        {
            case ModifierCategory.LegendaryNormal:
                prefabToUse = cardNormalWorldPrefab;
                break;
            case ModifierCategory.LegendaryOther:
                prefabToUse = cardOtherWorldPrefab;
                break;
                // Podés agregar ShiftWorld si sumás legendarios de esa categoría
        }

        // Instanciar la carta legendaria
        GameObject go = Instantiate(prefabToUse, cardContainer);
        //var cardUI = go.GetComponent<ModifierCardUI>();

        // Asignar acción al botón: cerrar panel
       // cardUI.Setup(legendaryModifier, () => {
          //  panel.SetActive(false);
        //}, 1);

        //spawnedCards.Add(go);

    }

}
