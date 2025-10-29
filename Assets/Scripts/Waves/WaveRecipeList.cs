using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Waves/WaveRecipeList")]
public class WaveRecipeList : ScriptableObject
{
    public List<WaveRecipe> waveRecipes;
}
