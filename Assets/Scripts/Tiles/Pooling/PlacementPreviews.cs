using UnityEngine;

public enum PreviewStatus
{
    Valid,
    Overlap,
    Invalid
}

public struct PlacementPreview
{
    public Vector3 origin;          // posición mundial donde se ubicaría el tile
    public Vector2 sizeXZ;          // tamaño visual del tile (w x h)
    public bool valid;              // true si no se superpone con otro tile
    public PreviewStatus status;    // estado (válido, solapado, inválido)
    public string note;             // texto descriptivo (ej: nombre, rotación)
    public float cellSize;          // tamaño de celda usado (solo informativo)
}
