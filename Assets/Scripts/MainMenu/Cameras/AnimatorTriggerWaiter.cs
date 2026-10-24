using System.Collections;
using UnityEngine;

public class AnimatorTriggerWaiter : IAnimationWaiter
{
    public IEnumerator TriggerAndWait(Animator animator, string triggerName, float timeoutSeconds = 0f)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            yield break;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        yield return null;

        float elapsed = 0f;

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
        if (animator.layerCount == 0) return true;

        int layer = 0;
        if (animator.IsInTransition(layer)) return false;

        var state = animator.GetCurrentAnimatorStateInfo(layer);

        if (state.loop) return true;

        return state.normalizedTime >= 1f;
    }
}
