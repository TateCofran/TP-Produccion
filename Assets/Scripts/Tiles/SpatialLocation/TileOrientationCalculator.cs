using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calcula orientación (rot/flip) y provee utilidades de conversión
/// de celdas a posiciones locales. Incluye caché para evitar recálculos.
/// </summary>
[DisallowMultipleComponent]
public sealed class TileOrientationCalculator : MonoBehaviour
{
    // Cachea por referencia de layout + rot + flip
    private readonly Dictionary<(TileLayout layout, int rot, bool flip), OrientedData> _orientationCache
        = new Dictionary<(TileLayout, int, bool), OrientedData>();

    /// <summary>
    /// Devuelve dimensiones orientadas y la entry orientada (con caché).
    /// </summary>
    public OrientedData GetOrientedData(TileLayout layout, int rotSteps, bool flip)
    {
        var key = (layout, rotSteps, flip);
        if (_orientationCache.TryGetValue(key, out var cached))
            return cached;

        OrientedSize(layout, rotSteps, out int w, out int h);
        Vector2Int entryOriented = ApplyOrientationToCell(layout.entry, layout, rotSteps, flip);

        var result = new OrientedData { w = w, h = h, entryOriented = entryOriented };
        _orientationCache[key] = result;
        return result;
    }

    /// <summary>
    /// Devuelve w/h del layout tras aplicar rotación (flip no afecta tamaño).
    /// </summary>
    public static void OrientedSize(TileLayout layout, int rotSteps, out int w, out int h)
    {
        bool swap = (rotSteps % 2) != 0;
        w = swap ? layout.gridHeight : layout.gridWidth;
        h = swap ? layout.gridWidth : layout.gridHeight;
    }

    /// <summary>
    /// Aplica rotación+flip a una celda en coords de layout y devuelve la celda en coords orientadas.
    /// </summary>
    public static Vector2Int ApplyOrientationToCell(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
    {
        int W = layout.gridWidth;
        int H = layout.gridHeight;
        Vector2 p = new Vector2(c.x, c.y);

        if (flip) p.x = (W - 1) - p.x;

        // Rotación de 90° horario repetida rotSteps veces:
        // (x,y) -> (y, W-1-x), y swap(W,H) en cada paso
        for (int i = 0; i < rotSteps; i++)
        {
            float x = p.x;
            p.x = p.y;
            p.y = (W - 1) - x;
            int tmp = W; W = H; H = tmp;
        }

        return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
    }

    /// <summary>
    /// Aplica rotación+flip a un vector dirección (local->mundo).
    /// </summary>
    public static Vector2Int ApplyOrientationToDir(Vector2Int d, int rotSteps, bool flip)
    {
        Vector2 p = new Vector2(d.x, d.y);
        if (flip) p.x = -p.x;

        // 90° horario: (x, y) -> (y, -x)
        for (int i = 0; i < rotSteps; i++)
        {
            float x = p.x;
            p.x = p.y;
            p.y = -x;
        }

        return new Vector2Int(
            Mathf.RoundToInt(Mathf.Clamp(p.x, -1, 1)),
            Mathf.RoundToInt(Mathf.Clamp(p.y, -1, 1))
        );
    }

    /// <summary>
    /// Aplica la inversa (mundo->local) a un vector dirección.
    /// Útil para comparar direcciones desde el tile previo.
    /// </summary>
    public static Vector2Int ApplyInverseOrientationToDir(Vector2Int d, int rotSteps, bool flip)
    {
        Vector2 p = new Vector2(d.x, d.y);

        // Inversa de la rotación 90° horario es 90° antihorario: (x, y) -> (-y, x)
        for (int i = 0; i < rotSteps; i++)
        {
            float x = p.x;
            p.x = -p.y;
            p.y = x;
        }

        if (flip) p.x = -p.x;

        return new Vector2Int(
            Mathf.RoundToInt(Mathf.Clamp(p.x, -1, 1)),
            Mathf.RoundToInt(Mathf.Clamp(p.y, -1, 1))
        );
    }

    /// <summary>
    /// Centro de la celda (orientada) en coords locales del tile (XZ).
    /// </summary>
    public static Vector3 CellToWorldLocal(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
    {
        Vector2Int oc = ApplyOrientationToCell(c, layout, rotSteps, flip);
        return new Vector3(oc.x * layout.cellSize, 0f, oc.y * layout.cellSize);
    }

    public void ClearCache() => _orientationCache.Clear();
}
