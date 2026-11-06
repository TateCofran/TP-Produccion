using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TilePlacementSequencer : MonoBehaviour
{
    [Header("Delays")]
    [Tooltip("Delay between tiles (Tile_X groups) when PlayForGroup is called with an accumulated startDelay.")]
    [SerializeField] private float perTileStartDelay = 0.0f;

    [Tooltip("Delay between cells inside the same tile (group children).")]
    [SerializeField] private float perCellDelay = 0.03f;

    [Tooltip("If enabled, shuffles the children order before animating.")]
    [SerializeField] private bool shuffleChildren = false;

    [Header("Dust")]
    [Tooltip("Small dust puff spawned when each cell lands. Pooled with VfxPool.")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField, Min(0.1f)] private float dustLifetime = 1f;

    public void PlayForGroup(Transform tileGroup, float startDelay = 0f)
    {
        if (tileGroup == null) return;
        StartCoroutine(Co_PlayGroup(tileGroup, startDelay, null, 1f));
    }

    public void PlayForGroup(Transform tileGroup, float startDelay, Vector3 entryWorld, float cellSize)
    {
        if (tileGroup == null) return;
        StartCoroutine(Co_PlayGroup(tileGroup, startDelay, entryWorld, Mathf.Max(0.01f, cellSize)));
    }

    private IEnumerator Co_PlayGroup(Transform group, float startDelay, Vector3? entryWorld, float cellSize)
    {
        var toAnimate = new List<Transform>(group.childCount);
        for (int i = 0; i < group.childCount; i++)
        {
            var child = group.GetChild(i);
            toAnimate.Add(child);
            var pre = child.GetComponent<TilePlacementAnimator>();
            if (pre != null && startDelay > 0f) child.localScale = Vector3.zero;
        }

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (entryWorld.HasValue)
        {
            Vector3 entry = entryWorld.Value;
            foreach (var tr in toAnimate)
            {
                if (tr == null) continue;
                var anim = tr.GetComponent<TilePlacementAnimator>();
                if (anim == null) continue;
                Vector3 d = tr.position - entry;
                float cells = (Mathf.Abs(d.x) + Mathf.Abs(d.z)) / cellSize;
                anim.Play(cells * perCellDelay, dustPrefab, dustLifetime);
            }
            yield break;
        }

        if (shuffleChildren)
        {
            for (int i = 0; i < toAnimate.Count; i++)
            {
                int j = Random.Range(i, toAnimate.Count);
                (toAnimate[i], toAnimate[j]) = (toAnimate[j], toAnimate[i]);
            }
        }

        float acc = 0f;
        foreach (var tr in toAnimate)
        {
            if (tr == null) continue;
            var anim = tr.GetComponent<TilePlacementAnimator>();
            if (anim != null)
                anim.Play(acc, dustPrefab, dustLifetime);
            acc += perCellDelay;
        }
    }

    public float GetNextTileStartDelay(int tileIndexFromZero)
    {
        return perTileStartDelay * Mathf.Max(0, tileIndexFromZero);
    }
}
