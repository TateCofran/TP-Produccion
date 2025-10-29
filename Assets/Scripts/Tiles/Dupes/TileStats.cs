// Ejemplo de TileStats.cs (si no existe)
using UnityEngine;

public class TileStats : MonoBehaviour
{
    [SerializeField] private float value;
    [SerializeField] private float spawnChance;
    [SerializeField] private float size;

    public void ApplyLevelModifiers(float finalValue, float finalSpawnChance, float finalSize)
    {
        value = finalValue;
        spawnChance = finalSpawnChance;
        size = finalSize;
    }
}
