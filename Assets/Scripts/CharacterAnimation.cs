using UnityEngine;

public static class CharacterAnimation
{
    public static void ApplyCharacterAnimation(GameObject playerObject, string characterName)
    {
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component missing on " + playerObject.name);
            return;
        }

        // Reset animator before applying the new one
        animator.runtimeAnimatorController = null;
        animator.Rebind();
        animator.Update(0);

        string overridePath = "Animations/" + characterName + "Override";
        AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);

        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            Debug.LogError("Override Controller not found for " + characterName);
        }
    }

    public static void PlayHitAnimation(GameObject playerObject)
    {
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
    public static void PlayDeathAnimation(GameObject playerObject)
    {
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
    }
}