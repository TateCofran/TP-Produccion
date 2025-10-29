// IAnimationWaiter.cs
using System.Collections;
using UnityEngine;

public interface IAnimationWaiter
{
    /// <summary>
    /// Dispara un trigger y espera a que la animación active finalice.
    /// Si timeoutSeconds > 0, corta la espera al exceder ese tiempo.
    /// </summary>
    IEnumerator TriggerAndWait(Animator animator, string triggerName, float timeoutSeconds = 0f);
}
