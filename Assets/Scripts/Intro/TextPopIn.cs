using UnityEngine;
using TMPro;

public class TextPopIn : MonoBehaviour
{
    public TextMeshProUGUI texto;
    public float duration = 0.75f; 
    

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = texto.transform.localScale;
    }

    public void PlayPopIn()
    {
        StopAllCoroutines();
        StartCoroutine(PopInRoutine());
    }

    private System.Collections.IEnumerator PopInRoutine()
    {
        texto.ForceMeshUpdate();
        texto.alpha = 0f;
        texto.transform.localScale = originalScale * 0.85f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            texto.alpha = Mathf.Lerp(0f, 1f, t);

            float s = Mathf.SmoothStep(0.85f, 1f, t);
            texto.transform.localScale = originalScale * s;

            yield return null;
        }

        texto.alpha = 1f;
        texto.transform.localScale = originalScale;
    }
}
