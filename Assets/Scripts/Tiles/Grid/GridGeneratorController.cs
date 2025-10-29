using UnityEngine;

public class GridGeneratorController : MonoBehaviour
{
    [SerializeField] private GridGenerator gridGenerator;

    public void OnGenerateFirstClicked() => gridGenerator?.UI_GenerateFirst();
    public void OnAppendNextClicked() => gridGenerator?.UI_AppendNext();
    public void OnCleanupClicked() => gridGenerator?.UI_CleanupCaches();
}