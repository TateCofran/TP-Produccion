using System.Collections.Generic;

[System.Serializable]
public class WaveStep
{
    public EnemyType enemyType;
    public int count;
    public float interval;
    public float waitAfterStep;
}

[System.Serializable]
public class WaveRecipe
{
    public int waveNumber;
    public List<WaveStep> steps = new List<WaveStep>();
}
