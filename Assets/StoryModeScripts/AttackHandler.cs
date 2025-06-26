using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AttackHandler
{
    // ... (Fields and constructor are unchanged)
    private MonoBehaviour monoBehaviourInstance;
    private GameObject player;
    private List<GameObject> enemies;
    private List<float> currentEnemyHealths;
    private Func<string[]> getEnemyNames;

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
    private Dictionary<StatusEffectType, StatusEffect> playerStatusEffects;
    private Action<string> updateBattleStatus;
    private class StatusUpdateAction
    {
        public string Message;
        public Coroutine AnimationCoroutine;
    }
    private RuntimeAnimatorController _playerOriginalAnimatorController;

    private readonly Queue<StatusUpdateAction> statusUpdateQueue = new Queue<StatusUpdateAction>();
    private bool isProcessingQueue = false;

    public AttackHandler(MonoBehaviour monoBehaviourInstance, GameObject player, List<GameObject> enemies, List<float> currentEnemyHealths, Func<string[]> getEnemyNames,
                         Func<float> getCurrentPlayerHealth, Action<float> setCurrentPlayerHealth, Func<float> getPlayerMaxHealth,
                         Func<float> getDamageMultiplier, Func<float> getCritChanceBonus, Func<float> getAccuracyBonus,
                         Func<float> getHealingMultiplier, Func<float> getDoubleEdgeReduction, Func<bool> getHasLifesteal,
                         Func<float> getLifestealPercentage, Func<bool> getHasShield, Action<float> setShieldAmount,
                         Dictionary<StatusEffectType, StatusEffect> playerStatusEffects,
                         Action<string> updateBattleStatus,
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
        this.playerStatusEffects = playerStatusEffects;
        this.updateBattleStatus = updateBattleStatus;
        this.moveSpeed = moveSpeed;
        this.attackDistance = attackDistance;
        this.originalPlayerPosition = originalPlayerPosition;

        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            _playerOriginalAnimatorController = playerAnimator.runtimeAnimatorController;
        }
    }


    public IEnumerator PerformAttack(AttackData attack, int primaryTargetIndex,
                                     Action<GameObject, AttackData> playImpactEffectCallback,
                                     Func<IEnumerator> flashPlayerSpriteCallback)
    {
        // ... (Accuracy check and Animator setup are unchanged) ...
        float modifiedAccuracy = Mathf.Min(1f, attack.accuracy + getAccuracyBonus());
        if (UnityEngine.Random.Range(0f, 1f) > modifiedAccuracy)
        {
            QueueStatusUpdate(attack.attackName + " missed!");
            yield return HandleMissedAttack(attack, flashPlayerSpriteCallback);
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
            yield break;
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator == null) yield break;

        RuntimeAnimatorController targetOriginalController = _playerOriginalAnimatorController;
        AnimatorOverrideController attackCharacterOverrideController = Resources.Load<AnimatorOverrideController>("Animations/" + attack.characterName + "Override");
        bool controllerWasTemporarilyChanged = false;
        if (attackCharacterOverrideController != null && animator.runtimeAnimatorController != attackCharacterOverrideController)
        {
            animator.runtimeAnimatorController = attackCharacterOverrideController;
            animator.Rebind();
            animator.Update(0f);
            controllerWasTemporarilyChanged = true;
        }

        if (attack.attackType == AttackType.Heal)
        {
            animator.SetTrigger(attack.animationTrigger);
            yield return new WaitForSeconds(attack.effectDelay);
            HandleHealAttack(attack);
            if (player != null) playImpactEffectCallback(player, attack);
            yield return monoBehaviourInstance.StartCoroutine(WaitForAnimationToComplete(animator, attack.animationTrigger, attack.effectDelay));
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
            yield break;
        }

        List<int> finalTargetIndices = new List<int>();
        var sbm = (StoryBattleManager)monoBehaviourInstance;
        List<int> aliveEnemies = sbm.GetAliveEnemyIndices();

        if (attack.numberOfTargets >= 99 || attack.numberOfTargets >= aliveEnemies.Count)
        {
            finalTargetIndices.AddRange(aliveEnemies);
            QueueStatusUpdate("You used " + attack.attackName + " on all enemies!");
        }
        else
        {
            if (primaryTargetIndex != -1 && aliveEnemies.Contains(primaryTargetIndex))
            {
                finalTargetIndices.Add(primaryTargetIndex);
            }

            if (attack.numberOfTargets > 1)
            {
                List<int> otherAliveEnemies = aliveEnemies.Where(i => i != primaryTargetIndex).ToList();
                int targetsToAdd = Mathf.Min(attack.numberOfTargets - finalTargetIndices.Count, otherAliveEnemies.Count);

                for (int i = 0; i < targetsToAdd; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, otherAliveEnemies.Count);
                    finalTargetIndices.Add(otherAliveEnemies[randomIndex]);
                    otherAliveEnemies.RemoveAt(randomIndex);
                }
            }
        }

        if (finalTargetIndices.Count == 0)
        {
            QueueStatusUpdate(attack.attackName + " had no valid targets!");
            yield break;
        }

        if (attack.attackType == AttackType.MoveAndHit)
        {
            yield return monoBehaviourInstance.StartCoroutine(MoveToEnemyAndAttack(attack, primaryTargetIndex, finalTargetIndices, playImpactEffectCallback));
        }
        else
        {
            animator.SetTrigger(attack.animationTrigger);
            yield return new WaitForSeconds(attack.effectDelay);
            if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
                GameManager.Instance.PlaySFX(attack.soundEffectName);

            // --- CHANGE --- Pass primaryTargetIndex to the damage handler
            yield return HandleDamageToTargets(attack, finalTargetIndices, primaryTargetIndex, playImpactEffectCallback);
            yield return monoBehaviourInstance.StartCoroutine(WaitForAnimationToComplete(animator, attack.animationTrigger, attack.effectDelay));
        }

        yield return HandleSelfDamage(attack, flashPlayerSpriteCallback);
        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (controllerWasTemporarilyChanged)
        {
            animator.runtimeAnimatorController = targetOriginalController;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private IEnumerator MoveToEnemyAndAttack(AttackData attack, int primaryTargetIndex, List<int> allTargetIndices, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        if (primaryTargetIndex < 0 || primaryTargetIndex >= enemies.Count || enemies[primaryTargetIndex] == null || !enemies[primaryTargetIndex].activeInHierarchy)
        {
            QueueStatusUpdate("Invalid primary target for move!");
            yield break;
        }

        Vector3 enemyPos = enemies[primaryTargetIndex].transform.position;
        Vector3 targetPos = enemyPos + Vector3.left * attackDistance;

        yield return monoBehaviourInstance.StartCoroutine(MovePlayerTo(targetPos));

        Animator animator = player.GetComponent<Animator>();
        if (animator != null) animator.SetTrigger(attack.animationTrigger);

        yield return new WaitForSeconds(attack.effectDelay);

        if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
            GameManager.Instance.PlaySFX(attack.soundEffectName);

        // --- CHANGE --- Pass primaryTargetIndex to the damage handler
        yield return HandleDamageToTargets(attack, allTargetIndices, primaryTargetIndex, playImpactEffectCallback);

        yield return monoBehaviourInstance.StartCoroutine(WaitForAnimationToComplete(animator, attack.animationTrigger, attack.effectDelay));
        yield return monoBehaviourInstance.StartCoroutine(MovePlayerTo(originalPlayerPosition));
    }

    // --- MAJOR CHANGE IN THIS METHOD ---
    private IEnumerator HandleDamageToTargets(AttackData attack, List<int> targetIndices, int primaryTargetIndex, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        var sbm = (StoryBattleManager)monoBehaviourInstance;
        float totalDamageDealt = 0f;

        foreach (int enemyIndex in targetIndices)
        {
            if (enemyIndex < 0 || enemyIndex >= currentEnemyHealths.Count || currentEnemyHealths[enemyIndex] <= 0 || !enemies[enemyIndex].activeInHierarchy) continue;

            // --- NEW LOGIC: Determine per-target multiplier ---
            float perTargetMultiplier = 1.0f;
            // Apply the penalty only if this is NOT the primary target
            if (enemyIndex != primaryTargetIndex)
            {
                perTargetMultiplier = attack.damageMultiplier;
            }

            // Apply the per-target multiplier, then the global player multiplier
            float finalDamage = (attack.damage * perTargetMultiplier + attack.damageIncrease) * getDamageMultiplier();
            float modifiedCritChance = Mathf.Min(1f, attack.critChance + getCritChanceBonus());
            bool isCrit = UnityEngine.Random.Range(0f, 1f) < modifiedCritChance;

            if (isCrit)
            {
                finalDamage *= 2f;
                if (GameManager.Instance != null) GameManager.Instance.PlayCritSound();
            }

            float initialHealth = currentEnemyHealths[enemyIndex];
            currentEnemyHealths[enemyIndex] = Mathf.Max(0, currentEnemyHealths[enemyIndex] - finalDamage);
            float damageDone = initialHealth - currentEnemyHealths[enemyIndex];
            totalDamageDealt += damageDone;

            playImpactEffectCallback(enemies[enemyIndex], attack);
            Coroutine healthChangeAnim = (damageDone > 0) ? sbm.uiManager.ShowEnemyHealthChange(enemyIndex, currentEnemyHealths[enemyIndex], -damageDone, isCrit) : null;

            if (targetIndices.Count == 1)
            {
                QueueStatusUpdate("You used " + attack.attackName + " on " + getEnemyNames()[enemyIndex] + (isCrit ? "! It's a critical hit!" : "!"), healthChangeAnim);
            }
        }

        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (getHasLifesteal() && getLifestealPercentage() > 0 && totalDamageDealt > 0)
        {
            float healAmount = totalDamageDealt * (getLifestealPercentage() / 100f);
            if (healAmount > 0)
            {
                float newPlayerHealth = Mathf.Min(getPlayerMaxHealth(), getCurrentPlayerHealth() + healAmount);
                setCurrentPlayerHealth(newPlayerHealth);
                QueueStatusUpdate($"Lifesteal healed {Mathf.Round(healAmount)} HP!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, healAmount, false));
                yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
            }
        }
    }

    // ... (The rest of the file is unchanged) ...
    private void QueueStatusUpdate(string message, Coroutine animationCoroutine = null)
    {
        statusUpdateQueue.Enqueue(new StatusUpdateAction { Message = message, AnimationCoroutine = animationCoroutine });
        if (!isProcessingQueue)
        {
            monoBehaviourInstance.StartCoroutine(ProcessStatusQueue());
        }
    }

    private IEnumerator ProcessStatusQueue()
    {
        isProcessingQueue = true;
        while (statusUpdateQueue.Count > 0)
        {
            StatusUpdateAction action = statusUpdateQueue.Dequeue();
            updateBattleStatus(action.Message);

            if (action.AnimationCoroutine != null)
            {
                yield return action.AnimationCoroutine;
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
        isProcessingQueue = false;
    }

    private IEnumerator WaitForQueue()
    {
        while (isProcessingQueue)
        {
            yield return null;
        }
    }

    private IEnumerator HandleSelfDamage(AttackData attack, Func<IEnumerator> flashPlayerSpriteCallback)
    {
        var sbm = (StoryBattleManager)monoBehaviourInstance;

        if (attack.doubleEdgeDamage > 0)
        {
            float reducedDamage = attack.doubleEdgeDamage * (1f - getDoubleEdgeReduction());
            float newPlayerHealth = Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage);
            setCurrentPlayerHealth(newPlayerHealth);
            QueueStatusUpdate("But you hurt yourself for " + Mathf.Round(reducedDamage) + " damage!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, -reducedDamage, false));
            yield return flashPlayerSpriteCallback();
        }

        if (attack.canSelfKO && UnityEngine.Random.Range(0f, 1f) < attack.selfKOFailChance)
        {
            setCurrentPlayerHealth(0);
            QueueStatusUpdate("The power was too great! You have been knocked out!", sbm.uiManager.ShowPlayerHealthChange(0, -999, false));
        }
    }

    private IEnumerator WaitForAnimationToComplete(Animator animator, string animationStateName, float alreadyWaited = 0f)
    {
        yield return null;
        float timer = 0f;
        const float timeout = 2.0f;

        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(animationStateName) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (animator != null && timer < timeout)
        {
            float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            float wait_time = animationLength - alreadyWaited;
            if (wait_time > 0)
            {
                yield return new WaitForSeconds(wait_time);
            }
        }
    }

    private IEnumerator MovePlayerTo(Vector3 targetPosition)
    {
        Vector3 startPosition = player.transform.position;
        float journey = 0f;
        float duration = Vector3.Distance(startPosition, targetPosition) / (moveSpeed * 1.5f);

        if (duration <= 0)
        {
            player.transform.position = targetPosition;
            yield break;
        }

        while (journey < duration)
        {
            journey += Time.deltaTime;
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, journey / duration);
            yield return null;
        }

        player.transform.position = targetPosition;
    }

    private void HandleHealAttack(AttackData attack)
    {
        float currentPlayerHealthValue = getCurrentPlayerHealth();
        float playerMaxHealthValue = getPlayerMaxHealth();

        float baseHealAmount = attack.damage * getHealingMultiplier();
        float healAmount = Mathf.Min(baseHealAmount, playerMaxHealthValue - currentPlayerHealthValue);

        if (healAmount > 0)
        {
            float newHealth = currentPlayerHealthValue + healAmount;
            setCurrentPlayerHealth(newHealth);
            var sbm = (StoryBattleManager)monoBehaviourInstance;
            QueueStatusUpdate("You used " + attack.attackName + " and recovered " + Mathf.Round(healAmount) + " HP!", sbm.uiManager.ShowPlayerHealthChange(newHealth, healAmount, false));
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX(attack.soundEffectName);
    }

    private IEnumerator HandleMissedAttack(AttackData attack, Func<IEnumerator> flashPlayerSpriteCallback)
    {
        yield return new WaitForSeconds(0.5f);

        if (attack.doubleEdgeDamage > 0)
        {
            var sbm = (StoryBattleManager)monoBehaviourInstance;
            float reducedDamage = attack.doubleEdgeDamage;
            float newPlayerHealth = Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage);
            setCurrentPlayerHealth(newPlayerHealth);

            QueueStatusUpdate("You hurt yourself due to missed attack!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, -reducedDamage, false));

            yield return flashPlayerSpriteCallback();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("General/MissVoice");
    }

    public IEnumerator PerformEnemyAttack(AttackData attack, GameObject attackingEnemy,
                                          Action<GameObject, AttackData> playImpactEffectCallback,
                                          Func<IEnumerator> flashPlayerSpriteCallback)
    {
        string enemyDisplayName = attackingEnemy.name.Replace("(Clone)", "").Trim();
        QueueStatusUpdate($"{enemyDisplayName} uses {attack.attackName}!");
        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (UnityEngine.Random.Range(0f, 1f) > attack.accuracy)
        {
            QueueStatusUpdate($"{attack.attackName} missed!");
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
            yield break;
        }

        Animator enemyAnimator = attackingEnemy.GetComponent<Animator>();

        switch (attack.attackType)
        {
            case AttackType.MoveAndHit:
                yield return monoBehaviourInstance.StartCoroutine(EnemyMoveToPlayerAndAttack(attack, attackingEnemy, playImpactEffectCallback, flashPlayerSpriteCallback));
                break;
            case AttackType.Heal:
                if (enemyAnimator != null && !string.IsNullOrEmpty(attack.animationTrigger))
                {
                    enemyAnimator.SetTrigger(attack.animationTrigger);
                }
                HandleEnemyHeal(attack, attackingEnemy);
                break;
            default:
                if (enemyAnimator != null && !string.IsNullOrEmpty(attack.animationTrigger))
                {
                    enemyAnimator.SetTrigger(attack.animationTrigger);
                }
                yield return new WaitForSeconds(attack.effectDelay);
                yield return monoBehaviourInstance.StartCoroutine(HandleEnemyDamagePlayer(attack, attackingEnemy, playImpactEffectCallback, flashPlayerSpriteCallback));
                break;
        }

        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (attack.effectsToApply != null && attack.effectsToApply.Count > 0)
        {
            foreach (var effect in attack.effectsToApply)
            {
                if (!effect.isBuff)
                {
                    ApplyPlayerStatusEffect(effect);
                    QueueStatusUpdate($"You are affected by {effect.effectType}!");
                    yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
                }
            }
        }
    }

    private void ApplyPlayerStatusEffect(StatusEffect effect)
    {
        if (playerStatusEffects.ContainsKey(effect.effectType))
        {
            playerStatusEffects[effect.effectType].duration = effect.duration;
        }
        else
        {
            playerStatusEffects.Add(effect.effectType, new StatusEffect
            {
                effectType = effect.effectType,
                duration = effect.duration,
                value = effect.value,
                isBuff = effect.isBuff
            });
        }
    }

    private IEnumerator EnemyMoveToPlayerAndAttack(AttackData attack, GameObject attackingEnemy, Action<GameObject, AttackData> playImpactEffectCallback, Func<IEnumerator> flashPlayerSpriteCallback)
    {
        Vector3 originalEnemyPos = attackingEnemy.transform.position;
        Vector3 targetPos = player.transform.position + Vector3.right * (attackDistance + UnityEngine.Random.Range(0, 0.5f));

        yield return monoBehaviourInstance.StartCoroutine(MoveCharacterTo(attackingEnemy.transform, targetPos));

        Animator enemyAnimator = attackingEnemy.GetComponent<Animator>();
        if (enemyAnimator != null && !string.IsNullOrEmpty(attack.animationTrigger))
        {
            enemyAnimator.SetTrigger(attack.animationTrigger);
        }

        yield return new WaitForSeconds(attack.effectDelay);

        yield return monoBehaviourInstance.StartCoroutine(HandleEnemyDamagePlayer(attack, attackingEnemy, playImpactEffectCallback, flashPlayerSpriteCallback));

        yield return new WaitForSeconds(0.5f);
        yield return monoBehaviourInstance.StartCoroutine(MoveCharacterTo(attackingEnemy.transform, originalEnemyPos));
    }

    private IEnumerator MoveCharacterTo(Transform character, Vector3 targetPosition)
    {
        Vector3 startPosition = character.position;
        float journey = 0f;
        float duration = Vector3.Distance(startPosition, targetPosition) / (moveSpeed * 1.5f);
        if (duration <= 0) { character.position = targetPosition; yield break; }
        while (journey < duration)
        {
            journey += Time.deltaTime;
            character.position = Vector3.Lerp(startPosition, targetPosition, journey / duration);
            yield return null;
        }
        character.position = targetPosition;
    }

    private void HandleEnemyHeal(AttackData attack, GameObject attackingEnemy)
    {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
        {
            GameManager.Instance.PlaySFX(attack.soundEffectName);
        }

        int enemyIndex = enemies.IndexOf(attackingEnemy);
        if (enemyIndex == -1) return;

        float healAmount = attack.damage;
        var sbm = (StoryBattleManager)monoBehaviourInstance;
        float maxHealth = sbm.enemyMaxHealth * Mathf.Pow(sbm.waveProgressionMultiplier, sbm.currentWave - 1);
        float newHealth = Mathf.Min(maxHealth, currentEnemyHealths[enemyIndex] + healAmount);
        currentEnemyHealths[enemyIndex] = newHealth;

        QueueStatusUpdate($"{attackingEnemy.name.Replace("(Clone)", "")} heals for {Mathf.Round(healAmount)} HP!", sbm.uiManager.ShowEnemyHealthChange(enemyIndex, newHealth, healAmount, false));
    }

    private IEnumerator HandleEnemyDamagePlayer(AttackData attack, GameObject attackingEnemy, Action<GameObject, AttackData> playImpactEffectCallback, Func<IEnumerator> flashPlayerSpriteCallback)
    {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
        {
            GameManager.Instance.PlaySFX(attack.soundEffectName);
        }

        bool isCrit = UnityEngine.Random.Range(0f, 1f) < attack.critChance;
        float damageDealt = attack.damage;
        if (isCrit)
        {
            damageDealt *= 2f;
            if (GameManager.Instance != null) GameManager.Instance.PlayCritSound();
        }

        var sbm = (StoryBattleManager)monoBehaviourInstance;

        playImpactEffectCallback(player, attack);

        float currentShield = sbm.ShieldAmount;
        if (getHasShield() && currentShield > 0)
        {
            float damageToShield = Mathf.Min(currentShield, damageDealt);
            setShieldAmount(currentShield - damageToShield);
            damageDealt -= damageToShield;

            if (damageToShield > 0)
            {
                QueueStatusUpdate($"Your shield absorbed {Mathf.Round(damageToShield)} damage!", sbm.uiManager.ShowPlayerHealthChange(getCurrentPlayerHealth(), -damageToShield, false));
                yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
            }
        }

        if (damageDealt > 0)
        {
            float newPlayerHealth = getCurrentPlayerHealth() - damageDealt;
            setCurrentPlayerHealth(newPlayerHealth);

            QueueStatusUpdate($"{attackingEnemy.name.Replace("(Clone)", "")} hits for {Mathf.Round(damageDealt)} damage!" + (isCrit ? " It's a critical hit!" : ""), sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, -damageDealt, isCrit));
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

            yield return flashPlayerSpriteCallback();
        }
        else if (isCrit)
        {
            QueueStatusUpdate($"A critical hit, but your shield took the full blow!");
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
        }
    }
}