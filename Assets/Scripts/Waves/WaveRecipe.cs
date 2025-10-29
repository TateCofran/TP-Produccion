using System.Collections.Generic;

[System.Serializable]
public class WaveStep
{
    public EnemyType enemyType;
    public int count;                 // Cuántos enemigos de este tipo
    public float interval;            // Intervalo entre spawns de este tipo
    public float waitAfterStep;       // Esperar después de este step
}

[System.Serializable]
public class WaveRecipe
{
    public int waveNumber;
    public List<WaveStep> steps = new List<WaveStep>();
}
