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
    private string[] enemyNames;

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
    private Action<float> setShieldAmount; // Now an Action to set the shield amount

    private float moveSpeed;
    private float attackDistance;
    private Vector3 originalPlayerPosition;
    private GameObject attackEffectPrefab;

    public AttackHandler(MonoBehaviour monoBehaviourInstance, GameObject player, List<GameObject> enemies, List<float> currentEnemyHealths, string[] enemyNames,
                         Func<float> getCurrentPlayerHealth, Action<float> setCurrentPlayerHealth, Func<float> getPlayerMaxHealth,
                         Func<float> getDamageMultiplier, Func<float> getCritChanceBonus, Func<float> getAccuracyBonus,
                         Func<float> getHealingMultiplier, Func<float> getDoubleEdgeReduction, Func<bool> getHasLifesteal,
                         Func<float> getLifestealPercentage, Func<bool> getHasShield, Action<float> setShieldAmount, // Changed to Action
                         float moveSpeed, float attackDistance, Vector3 originalPlayerPosition, GameObject attackEffectPrefab)
    {
        this.monoBehaviourInstance = monoBehaviourInstance;
        this.player = player;
        this.enemies = enemies;
        this.currentEnemyHealths = currentEnemyHealths;
        this.enemyNames = enemyNames;

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
        this.setShieldAmount = setShieldAmount; // Assign the setter delegate

        this.moveSpeed = moveSpeed;
        this.attackDistance = attackDistance;
        this.originalPlayerPosition = originalPlayerPosition;
        this.attackEffectPrefab = attackEffectPrefab;
    }

    public IEnumerator PerformAttack(AttackData attack, int targetEnemyIndex,
                                     Action<string> updateBattleStatusCallback,
                                     Action<GameObject, AttackData> playImpactEffectCallback,
                                     Func<IEnumerator> flashPlayerSpriteCallback)
    {
        float modifiedAccuracy = Mathf.Min(1f, attack.accuracy + getAccuracyBonus());
        if (UnityEngine.Random.Range(0f, 1f) > modifiedAccuracy)
        {
            updateBattleStatusCallback(attack.attackName + " missed!");
            yield return HandleMissedAttack(attack, flashPlayerSpriteCallback);
            yield break;
        }

        if (attack.attackType == AttackType.Heal)
        {
            HandleHealAttack(attack, updateBattleStatusCallback);
            yield return ShowAttackAnimation(player.GetComponent<Animator>(), attack, -1, playImpactEffectCallback);
        }
        else if (attack.attackType == AttackType.AreaEffect)
        {
            yield return HandleAreaAttack(attack, updateBattleStatusCallback, playImpactEffectCallback);
        }
        else if (attack.attackType == AttackType.MoveAndHit)
        {
            yield return MoveToEnemyAndAttack(attack, targetEnemyIndex, updateBattleStatusCallback, playImpactEffectCallback);
        }
        else // Single target damage attacks
        {
            HandleDamageAttack(attack, targetEnemyIndex, updateBattleStatusCallback);
            yield return ShowAttackAnimation(player.GetComponent<Animator>(), attack, targetEnemyIndex, playImpactEffectCallback);
        }
    }

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

        // Wait for player animation before impact effects
        yield return ShowAttackAnimation(player.GetComponent<Animator>(), attack, -1, playImpactEffectCallback); // Pass -1 for no specific target visual effect

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

        yield return new WaitForSeconds(0.5f);
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

        HandleDamageAttack(attack, targetEnemyIndex, updateBattleStatusCallback);

        yield return ShowAttackAnimation(player.GetComponent<Animator>(), attack, targetEnemyIndex, playImpactEffectCallback);

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
        updateBattleStatusCallback("You used " + attack.attackName + " on " + enemyNames[targetEnemyIndex] + "!" + critText);

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
            GameManager.Instance.PlaySFX("Audio/SFX/General/Miss");
        // Removed the generic "HitSound" on miss, as it might be confusing.
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack, int targetEnemyIndex, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        yield return new WaitForSeconds(attack.effectDelay);

        GameObject targetObject = null;
        if (attack.attackType == AttackType.Heal)
        {
            targetObject = player;
        }
        else if (targetEnemyIndex >= 0 && targetEnemyIndex < enemies.Count)
        {
            targetObject = enemies[targetEnemyIndex];
        }

        if (targetObject != null)
        {
            playImpactEffectCallback(targetObject, attack); // Call the UIManager's impact effect directly
        }

        // Removed the direct Instantiate/Destroy of attack effect prefab here, 
        // as UIManager.PlayImpactEffect handles it based on the attack data.
        yield return new WaitForSeconds(0.5f); // General pause for animation
    }
}