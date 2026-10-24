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

    public void PlayForGroup(Transform tileGroup, float startDelay = 0f)
    {
        if (tileGroup == null) return;
        StartCoroutine(Co_PlayGroup(tileGroup, startDelay));
    }

    private IEnumerator Co_PlayGroup(Transform group, float startDelay)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        var toAnimate = new List<Transform>(group.childCount);
        for (int i = 0; i < group.childCount; i++)
        {
            var child = group.GetChild(i);
            toAnimate.Add(child);
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
                anim.Play(acc);
            acc += perCellDelay;
        }
    }

    public float GetNextTileStartDelay(int tileIndexFromZero)
    {
        return perTileStartDelay * Mathf.Max(0, tileIndexFromZero);
    }
}
