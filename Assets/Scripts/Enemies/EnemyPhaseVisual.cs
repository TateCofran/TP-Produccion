using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyPhaseVisual : MonoBehaviour
{
    [Tooltip("Material used while the enemy cannot be damaged in the current world (a transparent ghost material).")]
    [SerializeField] private Material phasedMaterial;
    [Tooltip("Renderers to swap. If empty, all mesh renderers in children are used.")]
    [SerializeField] private Renderer[] renderers;
    [Tooltip("Objects hidden while phased (for example an outline or a shadow blob).")]
    [SerializeField] private GameObject[] hideWhilePhased;

    private Enemy enemy;
    private Material[][] originalMaterials;
    private Material[][] phasedMaterials;
    private bool phased;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (renderers == null || renderers.Length == 0)
        {
            var found = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    found.Add(r);
            }
            renderers = found.ToArray();
        }

        originalMaterials = new Material[renderers.Length][];
        phasedMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            originalMaterials[i] = renderers[i].sharedMaterials;

            var ghost = new Material[originalMaterials[i].Length];
            for (int m = 0; m < ghost.Length; m++) ghost[m] = phasedMaterial;
            phasedMaterials[i] = ghost;
        }
    }

    private void OnEnable()
    {
        WorldState.OnWorldChanged += HandleWorldChanged;
        Refresh();
    }

    private void OnDisable()
    {
        WorldState.OnWorldChanged -= HandleWorldChanged;
        SetPhased(false);
    }

    private void HandleWorldChanged(ShiftingWorldMechanic.World world) => Refresh();

    public void Refresh()
    {
        if (enemy == null) return;
        SetPhased(!enemy.IsTargetable());
    }

    private void SetPhased(bool value)
    {
        if (phased == value) return;
        phased = value;

        if (phasedMaterial != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterials = value ? phasedMaterials[i] : originalMaterials[i];
            }
        }

        if (hideWhilePhased != null)
        {
            for (int i = 0; i < hideWhilePhased.Length; i++)
            {
                if (hideWhilePhased[i] != null)
                    hideWhilePhased[i].SetActive(!value);
            }
        }
    }
}
