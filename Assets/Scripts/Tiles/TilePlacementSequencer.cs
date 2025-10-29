// TilePlacementSequencer.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TilePlacementSequencer : MonoBehaviour
{
    [Header("Delays")]
    [Tooltip("Retraso entre tiles (grupos Tile_X) cuando se invoca PlayForGroup con startDelay acumulable).")]
    [SerializeField] private float perTileStartDelay = 0.0f;

    [Tooltip("Retraso entre celdas dentro del mismo tile (children del grupo).")]
    [SerializeField] private float perCellDelay = 0.03f;

    [Tooltip("Si true, baraja el orden de hijos antes de animar (aspecto más orgánico).")]
    [SerializeField] private bool shuffleChildren = false;

    /// <summary>
    /// Secuencia la animación de todos los hijos con un delay incremental.
    /// Llamalo pasando el 'parent' que devuelve InstantiateLayout (el grupo Tile_X).
    /// </summary>
    public void PlayForGroup(Transform tileGroup, float startDelay = 0f)
    {
        if (tileGroup == null) return;
        StartCoroutine(Co_PlayGroup(tileGroup, startDelay));
    }

    private IEnumerator Co_PlayGroup(Transform group, float startDelay)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // Recolectar hijos a animar
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

        // Lanzar animaciones con offset incremental
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

    /// <summary>
    /// Helper por si querés computar un delay acumulado entre tiles.
    /// </summary>
    public float GetNextTileStartDelay(int tileIndexFromZero)
    {
        return perTileStartDelay * Mathf.Max(0, tileIndexFromZero);
    }
}
