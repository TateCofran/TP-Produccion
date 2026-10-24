using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class TextPopIn : MonoBehaviour
{
    [FormerlySerializedAs("texto")]
    public TextMeshProUGUI text;
    public float duration = 0.75f; 
    

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = text.transform.localScale;
    }

    public void PlayPopIn()
    {
        StopAllCoroutines();
        StartCoroutine(PopInRoutine());
    }

    private System.Collections.IEnumerator PopInRoutine()
    {
        text.ForceMeshUpdate();
        text.alpha = 0f;
        text.transform.localScale = originalScale * 0.85f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            text.alpha = Mathf.Lerp(0f, 1f, t);

            float s = Mathf.SmoothStep(0.85f, 1f, t);
            text.transform.localScale = originalScale * s;

            yield return null;
        }

        text.alpha = 1f;
        text.transform.localScale = originalScale;
    }
}
