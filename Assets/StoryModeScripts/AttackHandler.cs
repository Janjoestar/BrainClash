using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AttackHandler
{
    private MonoBehaviour monoBehaviourInstance;
    private GameObject player;
    private List<GameObject> enemies;
    private List<float> currentEnemyHealths;
    private Func<string[]> getEnemyNames;

    // Use delegates for dynamic health and modifier access
    private Func<float> getCurrentPlayerHealth;
    private Action<float> setCurrentPlayerHealth;
    private Func<float> getPlayerMaxHealth;
    private Func<float> getDamageMultiplier;
    private Func<float> getCritChanceBonus;
    private Func<float> getAccuracyBonus;
    private Func<float> getHealingMultiplier;
    private Func<float> getDoubleEdgeReduction;
    private Func<bool> getHasLifesteal;
    private Func<float> getLifestealPercentage;
    private Func<bool> getHasShield;
    private Action<float> setShieldAmount;

    private float moveSpeed;
    private float attackDistance;
    private Vector3 originalPlayerPosition;
    private GameObject attackEffectPrefab; // This prefab is now handled by UIManager's callback

    // Store the player's original AnimatorController when AttackHandler is constructed
    private RuntimeAnimatorController _playerOriginalAnimatorController;

    public AttackHandler(MonoBehaviour monoBehaviourInstance, GameObject player, List<GameObject> enemies, List<float> currentEnemyHealths, Func<string[]> getEnemyNames,
                          Func<float> getCurrentPlayerHealth, Action<float> setCurrentPlayerHealth, Func<float> getPlayerMaxHealth,
                          Func<float> getDamageMultiplier, Func<float> getCritChanceBonus, Func<float> getAccuracyBonus,
                          Func<float> getHealingMultiplier, Func<float> getDoubleEdgeReduction, Func<bool> getHasLifesteal,
                          Func<float> getLifestealPercentage, Func<bool> getHasShield, Action<float> setShieldAmount,
                          float moveSpeed, float attackDistance, Vector3 originalPlayerPosition, GameObject attackEffectPrefab)
    {
        this.monoBehaviourInstance = monoBehaviourInstance;
        this.player = player;
        this.enemies = enemies;
        this.currentEnemyHealths = currentEnemyHealths;
        this.getEnemyNames = getEnemyNames;

        this.getCurrentPlayerHealth = getCurrentPlayerHealth;
        this.setCurrentPlayerHealth = setCurrentPlayerHealth;
        this.getPlayerMaxHealth = getPlayerMaxHealth;
        this.getDamageMultiplier = getDamageMultiplier;
        this.getCritChanceBonus = getCritChanceBonus;
        this.getAccuracyBonus = getAccuracyBonus;
        this.getHealingMultiplier = getHealingMultiplier;
        this.getDoubleEdgeReduction = getDoubleEdgeReduction;
        this.getHasLifesteal = getHasLifesteal;
        this.getLifestealPercentage = getLifestealPercentage;
        this.getHasShield = getHasShield;
        this.setShieldAmount = setShieldAmount;

        this.moveSpeed = moveSpeed;
        this.attackDistance = attackDistance;
        this.originalPlayerPosition = originalPlayerPosition;
        this.attackEffectPrefab = attackEffectPrefab;

        // Initialize _playerOriginalAnimatorController here
        // At this point, StoryBattleManager.InitializeStoryBattle() should have already set the player's initial animator.
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            _playerOriginalAnimatorController = playerAnimator.runtimeAnimatorController;
        }
        else
        {
            Debug.LogError("[AttackHandler] Player GameObject is missing an Animator component!");
        }
    }

    public IEnumerator PerformAttack(AttackData attack, int targetEnemyIndex,
                                      Action<string> updateBattleStatusCallback,
                                      Action<GameObject, AttackData> playImpactEffectCallback,
                                      Func<IEnumerator> flashPlayerSpriteCallback)
    {
        // Cooldown check and application removed

        float modifiedAccuracy = Mathf.Min(1f, attack.accuracy + getAccuracyBonus());
        if (UnityEngine.Random.Range(0f, 1f) > modifiedAccuracy)
        {
            updateBattleStatusCallback(attack.attackName + " missed!");
            yield return HandleMissedAttack(attack, flashPlayerSpriteCallback);
            yield break;
        }

        // Get animator here to ensure it's still valid
        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AttackHandler] Animator component missing on player.");
            yield break;
        }

        // Use the stored original controller as the target for reversion
        RuntimeAnimatorController targetOriginalController = _playerOriginalAnimatorController;

        AnimatorOverrideController attackCharacterOverrideController = null;
        string overridePath = "Animations/" + attack.characterName + "Override";

        // Try to load the override controller for the attack's character
        attackCharacterOverrideController = Resources.Load<AnimatorOverrideController>(overridePath);

        bool controllerWasTemporarilyChanged = false;

        // Check if the loaded override controller for the attack is actually different from the current one on the Animator
        if (attackCharacterOverrideController != null && animator.runtimeAnimatorController != attackCharacterOverrideController)
        {
            animator.runtimeAnimatorController = attackCharacterOverrideController;
            // Force the animator to reset and apply the new controller immediately.
            animator.Rebind();
            animator.Update(0f); // Ensure the state machine is initialized for the new controller
            controllerWasTemporarilyChanged = true;
            Debug.Log($"[AttackHandler] Temporarily switching animator to {attack.characterName} for {attack.attackName} (Original: {targetOriginalController?.name ?? "N/A"}).");
        }
        else if (attackCharacterOverrideController == null)
        {
            Debug.LogWarning($"[AttackHandler] Animator Override Controller not found for {attack.characterName} at {overridePath}. Using currently set animator. Animation may not play correctly.");
        }
        // If it's already the correct controller, do nothing.

        // Add a small delay BEFORE setting the trigger to ensure the controller switch has fully registered.
        yield return new WaitForSeconds(0.05f); // A very small buffer

        // Play the attack animation
        animator.SetTrigger(attack.animationTrigger);

        // --- RELIABLE ANIMATION WAITING ---
        yield return null; // Wait one frame for the trigger to activate a state

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = 0f;

        bool stateFound = false;
        float timer = 0f;
        float timeout = 2.0f; // Prevent infinite loop

        while (!stateFound && timer < timeout)
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName(attack.animationTrigger))
            {
                animationLength = stateInfo.length;
                stateFound = true;
            }
            else
            {
                // Iterate through all clips in the CURRENTLY ACTIVE controller to find a match by name.
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name.Equals(attack.animationTrigger))
                    {
                        animationLength = clip.length;
                        stateFound = true;
                        break;
                    }
                }
            }

            if (!stateFound)
            {
                yield return null; // Wait a frame and re-check
                timer += Time.deltaTime;
            }
        }

        if (!stateFound || animationLength <= 0f)
        {
            Debug.LogWarning($"[AttackHandler] Failed to get accurate animation length for '{attack.animationTrigger}' on '{attack.characterName}'. Fallback to 1.0s. (Controller: {animator.runtimeAnimatorController?.name})");
            animationLength = 1.0f; // Fallback to a safe duration
        }

        if (attack.attackType == AttackType.Heal)
        {
            HandleHealAttack(attack, updateBattleStatusCallback);
            // Effect comes directly after animation trigger delay
            yield return new WaitForSeconds(Mathf.Min(attack.effectDelay, animationLength));
            if (player != null) playImpactEffectCallback(player, attack); // Effect on player for heal
        }
        else if (attack.attackType == AttackType.AreaEffect)
        {
            // Effect comes directly after animation trigger delay
            yield return new WaitForSeconds(Mathf.Min(attack.effectDelay, animationLength));
            yield return HandleAreaAttack(attack, updateBattleStatusCallback, playImpactEffectCallback);
        }
        else if (attack.attackType == AttackType.MoveAndHit)
        {
            // MoveToEnemyAndAttack handles its own animation and effects
            yield return MoveToEnemyAndAttack(attack, targetEnemyIndex, updateBattleStatusCallback, playImpactEffectCallback);
        }
        else // Single target damage attacks (Slash, Projectile, Magic, DirectHit)
        {
            // Calculate damage first
            HandleDamageAttack(attack, targetEnemyIndex, updateBattleStatusCallback);

            // Effect comes directly after animation trigger delay
            yield return new WaitForSeconds(Mathf.Min(attack.effectDelay, animationLength));
            if (targetEnemyIndex >= 0 && targetEnemyIndex < enemies.Count && enemies[targetEnemyIndex] != null)
            {
                playImpactEffectCallback(enemies[targetEnemyIndex], attack); // Effect on target enemy
            }
        }

        // Wait for the remainder of the animation to truly finish for the currently active controller
        if (animationLength > attack.effectDelay)
        {
            yield return new WaitForSeconds(animationLength - attack.effectDelay);
        }
        // If it was a MoveAndHit, the return movement is handled there.
        // For other attack types, add a small general pause after everything.
        if (attack.attackType != AttackType.MoveAndHit)
        {
            yield return new WaitForSeconds(0.2f); // Small pause for general flow
        }


        // --- REVERT ANIMATOR ---
        if (controllerWasTemporarilyChanged)
        {
            animator.runtimeAnimatorController = targetOriginalController; // Revert to the battle's starting character's controller
            animator.Rebind();
            animator.Update(0f); // Ensure it's re-initialized for the original character
            Debug.Log($"[AttackHandler] Reverted animator to original character: {targetOriginalController?.name ?? "N/A"}.");
        }
    }

    // Removed: public void DecrementAttackCooldowns(List<AttackData> playerAttacks)

    private IEnumerator HandleAreaAttack(AttackData attack, Action<string> updateBattleStatusCallback, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        List<int> aliveEnemies = new List<int>();
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            if (currentEnemyHealths[i] > 0 && enemies[i].activeInHierarchy) // Check if enemy is active
            {
                aliveEnemies.Add(i);
            }
        }

        updateBattleStatusCallback("You used " + attack.attackName + " on all enemies!");

        float totalDamageDealt = 0f;
        foreach (int enemyIndex in aliveEnemies)
        {
            float finalDamage = (attack.damage + attack.damageIncrease) * getDamageMultiplier(); // Apply damageIncrease
            bool isCrit = UnityEngine.Random.Range(0f, 1f) < (attack.critChance + getCritChanceBonus());

            if (isCrit)
            {
                finalDamage *= 2f;
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayCritSound();
            }

            currentEnemyHealths[enemyIndex] = Mathf.Max(0, currentEnemyHealths[enemyIndex] - finalDamage);
            totalDamageDealt += finalDamage;

            if (enemyIndex < enemies.Count && enemies[enemyIndex] != null)
            {
                // Play impact effect directly here for each enemy after damage is calculated
                playImpactEffectCallback(enemies[enemyIndex], attack);
            }
        }

        if (getHasLifesteal() && getLifestealPercentage() > 0)
        {
            float healAmount = totalDamageDealt * (getLifestealPercentage() / 100f);
            setCurrentPlayerHealth(Mathf.Min(getPlayerMaxHealth(), getCurrentPlayerHealth() + healAmount));
            if (healAmount > 0)
            {
                updateBattleStatusCallback($"Lifesteal healed {Mathf.Round(healAmount)} HP!");
            }
        }

        if (attack.doubleEdgeDamage > 0)
        {
            float currentShield = ((StoryBattleManager)monoBehaviourInstance).ShieldAmount; // Get current shield from manager
            float reducedDamage = attack.doubleEdgeDamage * (1f - getDoubleEdgeReduction());

            if (getHasShield() && currentShield > 0)
            {
                float damageToShield = Mathf.Min(currentShield, reducedDamage);
                setShieldAmount(currentShield - damageToShield); // Update shield via the setter
                reducedDamage -= damageToShield;
                updateBattleStatusCallback($"Shield absorbed {Mathf.Round(damageToShield)} damage!");
            }

            if (reducedDamage > 0)
            {
                setCurrentPlayerHealth(Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage));
                updateBattleStatusCallback("But you hurt yourself for " + Mathf.Round(reducedDamage) + " damage!");
            }
        }

        yield return new WaitForSeconds(0.5f); // General pause after all area effects are shown
    }

    private IEnumerator MoveToEnemyAndAttack(AttackData attack, int targetEnemyIndex, Action<string> updateBattleStatusCallback, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= enemies.Count || enemies[targetEnemyIndex] == null || !enemies[targetEnemyIndex].activeInHierarchy)
        {
            updateBattleStatusCallback("Invalid target!");
            yield break;
        }

        Vector3 enemyPos = enemies[targetEnemyIndex].transform.position;
        Vector3 targetPos = enemyPos + Vector3.left * attackDistance;

        yield return MovePlayerTo(targetPos);

        // Perform damage and play player attack animation here
        HandleDamageAttack(attack, targetEnemyIndex, updateBattleStatusCallback); // This will now also trigger impact effect via its own playImpactEffectCallback

        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        // Wait for the animation to finish before moving back
        float animationLength = 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(attack.animationTrigger))
        {
            animationLength = stateInfo.length;
        }
        else
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Equals(attack.animationTrigger))
                {
                    animationLength = clip.length;
                    break;
                }
            }
        }
        if (animationLength <= 0f) animationLength = 1.0f; // Fallback

        yield return new WaitForSeconds(animationLength); // Wait for entire animation, as effect is already handled in HandleDamageAttack

        yield return MovePlayerTo(originalPlayerPosition);
    }

    private IEnumerator MovePlayerTo(Vector3 targetPosition)
    {
        Vector3 startPosition = player.transform.position;
        float journey = 0f;
        float duration = Vector3.Distance(startPosition, targetPosition) / moveSpeed; // Calculate duration based on distance and speed

        while (journey < duration)
        {
            journey += Time.deltaTime;
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, journey / duration);
            yield return null;
        }

        player.transform.position = targetPosition;
    }

    private void HandleHealAttack(AttackData attack, Action<string> updateBattleStatusCallback)
    {
        float currentPlayerHealthValue = getCurrentPlayerHealth();
        float playerMaxHealthValue = getPlayerMaxHealth();

        float baseHealAmount = attack.damage * getHealingMultiplier(); // Apply healing multiplier
        float healAmount = Mathf.Min(baseHealAmount, playerMaxHealthValue - currentPlayerHealthValue);
        setCurrentPlayerHealth(currentPlayerHealthValue + healAmount);
        updateBattleStatusCallback("You used " + attack.attackName + " and recovered " + Mathf.Round(healAmount) + " HP!");

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX(attack.soundEffectName);
    }

    private void HandleDamageAttack(AttackData attack, int targetEnemyIndex, Action<string> updateBattleStatusCallback)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= currentEnemyHealths.Count || currentEnemyHealths[targetEnemyIndex] <= 0 || !enemies[targetEnemyIndex].activeInHierarchy)
        {
            updateBattleStatusCallback("Invalid target!");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX(attack.soundEffectName);

        float finalDamage = (attack.damage + attack.damageIncrease) * getDamageMultiplier(); // Apply damageIncrease
        float modifiedCritChance = Mathf.Min(1f, attack.critChance + getCritChanceBonus());
        bool isCrit = UnityEngine.Random.Range(0f, 1f) < modifiedCritChance;

        if (isCrit)
        {
            finalDamage *= 2f;
            if (GameManager.Instance != null)
                GameManager.Instance.PlayCritSound();
        }

        currentEnemyHealths[targetEnemyIndex] = Mathf.Max(0, currentEnemyHealths[targetEnemyIndex] - finalDamage);

        // This is where the impact effect happens for single-target damage attacks
        // The playImpactEffectCallback is passed into PerformAttack, so it's available here.
        // However, we are calling it in PerformAttack after the effectDelay.
        // If you want it to happen immediately on damage calculation, move it here.
        // For consistency with other types, we'll keep it in PerformAttack for now.
        // If it's a Projectile, Magic, or DirectHit, it will be handled in PerformAttack's "else" block.

        if (getHasLifesteal() && getLifestealPercentage() > 0)
        {
            float healAmount = finalDamage * (getLifestealPercentage() / 100f);
            setCurrentPlayerHealth(Mathf.Min(getPlayerMaxHealth(), getCurrentPlayerHealth() + healAmount));
            if (healAmount > 0)
            {
                updateBattleStatusCallback($"Lifesteal healed {Mathf.Round(healAmount)} HP!");
            }
        }

        string critText = isCrit ? " It's a critical hit!" : "";
        updateBattleStatusCallback("You used " + attack.attackName + " on " + getEnemyNames()[targetEnemyIndex] + "!" + critText); // Use the delegate here

        if (attack.doubleEdgeDamage > 0)
        {
            float currentShield = ((StoryBattleManager)monoBehaviourInstance).ShieldAmount; // Get current shield from manager
            float reducedDamage = attack.doubleEdgeDamage * (1f - getDoubleEdgeReduction());

            if (getHasShield() && currentShield > 0)
            {
                float damageToShield = Mathf.Min(currentShield, reducedDamage);
                setShieldAmount(currentShield - damageToShield); // Update shield via the setter
                reducedDamage -= damageToShield;
                updateBattleStatusCallback($"Shield absorbed {Mathf.Round(damageToShield)} damage!");
            }

            if (reducedDamage > 0)
            {
                setCurrentPlayerHealth(Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage));
                updateBattleStatusCallback("But you hurt yourself for " + Mathf.Round(reducedDamage) + " damage!");
            }
        }
    }

    private IEnumerator HandleMissedAttack(AttackData attack, Func<IEnumerator> flashPlayerSpriteCallback)
    {
        yield return new WaitForSeconds(1.0f);

        if (attack.doubleEdgeDamage > 0)
        {
            float currentShield = ((StoryBattleManager)monoBehaviourInstance).ShieldAmount; // Get current shield from manager
            float reducedDamage = attack.doubleEdgeDamage; // No reduction if attack missed

            if (getHasShield() && currentShield > 0)
            {
                float damageToShield = Mathf.Min(currentShield, reducedDamage);
                setShieldAmount(currentShield - damageToShield); // Update shield via the setter
                reducedDamage -= damageToShield;
                ((StoryBattleManager)monoBehaviourInstance).uiManager.UpdateBattleStatus($"Shield absorbed {Mathf.Round(damageToShield)} damage!");
            }

            if (reducedDamage > 0)
            {
                setCurrentPlayerHealth(Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage));

                Animator playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger("Hit");
                }
                yield return flashPlayerSpriteCallback();
                yield return new WaitForSeconds(0.5f);
            }
            ((StoryBattleManager)monoBehaviourInstance).uiManager.UpdateBattleStatus("You hurt yourself due to missed attack!");
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("General/MissVoice");
    }
}