// AnimatorTriggerWaiter.cs
using System.Collections;
using UnityEngine;

public class AnimatorTriggerWaiter : IAnimationWaiter
{
    public IEnumerator TriggerAndWait(Animator animator, string triggerName, float timeoutSeconds = 0f)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            yield break;

        // Disparar el trigger
        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        // Esperar 1 frame para que el Animator haga el cambio de estado
        yield return null;

        float elapsed = 0f;
        // Espera mientras haya una animación no looping en curso
        while (!HasFinished(animator))
        {
            if (timeoutSeconds > 0f)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= timeoutSeconds) break;
            }
            yield return null;
        }
    }

    private static bool HasFinished(Animator animator)
    {
        // Consideramos finalizada cuando:
        // 1) No está en transición y
        // 2) El estado actual (layer 0) no es looping y normalizedTime >= 1
        if (animator.layerCount == 0) return true;

        int layer = 0;
        if (animator.IsInTransition(layer)) return false;

        var state = animator.GetCurrentAnimatorStateInfo(layer);

        // Si loop, no esperamos (se considera finalizada para evitar bloqueo)
        if (state.loop) return true;

        return state.normalizedTime >= 1f;
    }
}
