using UnityEngine;

public enum PreviewStatus
{
    Valid,
    Overlap,
    Invalid
}

public struct PlacementPreview
{
    public Vector3 origin;
    public Vector2 sizeXZ;
    public bool valid;
    public PreviewStatus status;
    public string note;
    public float cellSize;
}
