using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TileLayoutGizmos : MonoBehaviour
{
    [Header("Fuente de estado")]
    public GridGenerator gridGenerator;

    [Header("Mostrar")]
    public bool drawLastTileAABB = true;
    public bool drawUnusedExits = true;
    public bool drawPlacementPreviews = true;
    public bool drawExitArrow = true;
    public bool showPreviewLabelsOnlyForValid = true;

    [Header("Colores")]
    public Color aabbWire = new Color(1f, 1f, 1f, 0.35f);
    public Color exitColor = Color.yellow;
    public Color okFill = new Color(0f, 1f, 0f, 0.18f);
    public Color overlapFill = new Color(1f, 0f, 0f, 0.22f);
    public Color edgeFill = new Color(1f, 0.6f, 0f, 0.22f);
    public Color previewWire = new Color(0f, 0f, 0f, 0.38f);

    [Header("Tamaños")]
    public float yLift = 0.02f;
    public float sphereRadius = 0.2f;
    public float labelHeight = 0.3f;
    public float arrowSize = 0.8f;

#if UNITY_EDITOR
    static GUIStyle _labelStyleMain;
    static GUIStyle _labelStyleShadow;
#endif

    void OnDrawGizmos()
    {
        if (!gridGenerator) return;

#if UNITY_EDITOR
        EnsureStyles();
#endif

        // AABB del último tile
        if (drawLastTileAABB && gridGenerator.CurrentLayout != null)
        {
            var L = gridGenerator.CurrentLayout;
            TileOrientationCalculator.OrientedSize(L, gridGenerator.CurrentRotSteps, out int w, out int h);
            float W = w * L.cellSize, H = h * L.cellSize;

            Vector3 o = gridGenerator.CurrentWorldOrigin;
            Vector3 center = o + new Vector3(W * 0.5f - L.cellSize * 0.5f, yLift, H * 0.5f - L.cellSize * 0.5f);
            Vector3 size = new Vector3(W, 0.01f, H);

            Gizmos.color = aabbWire;
            Gizmos.DrawWireCube(center, size);
        }

#if UNITY_EDITOR
        // flecha del exit seleccionado
        if (drawExitArrow && gridGenerator.TryGetSelectedExitWorld(out var exitPos, out var dir))
        {
            Handles.color = Color.white;
            Vector3 dir3 = new Vector3(dir.x, 0f, dir.y);
            Handles.ArrowHandleCap(0, exitPos + Vector3.up * (yLift + 0.03f), Quaternion.LookRotation(dir3, Vector3.up), arrowSize, EventType.Repaint);
            DrawLabel(exitPos + Vector3.up * (yLift + labelHeight), "EXIT sel.");
        }
#endif

        // exits no usados
        if (drawUnusedExits)
        {
            var exits = gridGenerator.GetAvailableExits();
            Gizmos.color = exitColor;
            foreach (var (label, pos) in exits)
            {
                Vector3 p = pos + Vector3.up * yLift;
                Gizmos.DrawSphere(p, sphereRadius);
#if UNITY_EDITOR
                DrawLabel(p + Vector3.up * labelHeight, label);
#endif
            }
        }

        // previews
        if (drawPlacementPreviews)
        {
            var previews = gridGenerator.GetPlacementPreviews();
            foreach (var pv in previews)
            {
                Vector3 center = pv.origin + new Vector3(pv.sizeXZ.x * 0.5f - pv.cellSize * 0.5f, yLift, pv.sizeXZ.y * 0.5f - pv.cellSize * 0.5f);
                Vector3 size = new Vector3(pv.sizeXZ.x, 0.01f, pv.sizeXZ.y);

                // usar el enum global PreviewStatus
                switch (pv.status)
                {
                    case PreviewStatus.Valid: Gizmos.color = okFill; break;
                    case PreviewStatus.Overlap: Gizmos.color = overlapFill; break;
                    default: Gizmos.color = edgeFill; break;
                }
                Gizmos.DrawCube(center, size);

                Gizmos.color = previewWire;
                Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
                if (!showPreviewLabelsOnlyForValid || pv.valid)
                {
                    string tag = pv.status == PreviewStatus.Valid ? "OK" :
                                 pv.status == PreviewStatus.Overlap ? "SOLAPA" : "BORDE";
                    DrawLabel(center + Vector3.up * (labelHeight * 0.6f), $"{tag}\n{pv.note}");
                }
#endif
            }
        }
    }

#if UNITY_EDITOR
    void EnsureStyles()
    {
        if (_labelStyleMain == null)
        {
            _labelStyleMain = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                richText = true,
                normal = { textColor = Color.white }
            };
            _labelStyleShadow = new GUIStyle(_labelStyleMain)
            {
                normal = { textColor = new Color(0, 0, 0, 0.75f) }
            };
        }
    }

    void DrawLabel(Vector3 worldPos, string text)
    {
        Handles.Label(worldPos + new Vector3(0.01f, 0.01f, 0), text, _labelStyleShadow);
        Handles.Label(worldPos, text, _labelStyleMain);
    }
#endif
}
