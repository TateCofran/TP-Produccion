using System.Collections;
using UnityEngine;

public interface IAnimationWaiter
{
    IEnumerator TriggerAndWait(Animator animator, string triggerName, float timeoutSeconds = 0f);
}
