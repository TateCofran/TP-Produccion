using UnityEngine;

[CreateAssetMenu(menuName = "Tiles/TileData")]
public class TileDataSO : ScriptableObject
{
    public string id;
    public float value = 1f;
    public float spawnChance = 1f;
    public float size = 1f;
}
