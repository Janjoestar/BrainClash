using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AttackHandler
{
    private class StatusUpdateAction
    {
        public string Message;
        public Coroutine AnimationCoroutine;
    }

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

    public IEnumerator PerformAttack(AttackData attack, int targetEnemyIndex,
                                     Action<GameObject, AttackData> playImpactEffectCallback,
                                     Func<IEnumerator> flashPlayerSpriteCallback)
    {
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

        if (attack.attackType == AttackType.MoveAndHit)
        {
            yield return monoBehaviourInstance.StartCoroutine(MoveToEnemyAndAttack(attack, targetEnemyIndex, playImpactEffectCallback));
        }
        else
        {
            animator.SetTrigger(attack.animationTrigger);
            // Use flashInterval for delay as it contains the correct timing value from the data
            yield return new WaitForSeconds(attack.flashInterval);

            if (attack.attackType == AttackType.Heal)
            {
                HandleHealAttack(attack);
                if (player != null) playImpactEffectCallback(player, attack);
            }
            else if (attack.attackType == AttackType.AreaEffect)
            {
                yield return monoBehaviourInstance.StartCoroutine(HandleAreaAttack(attack, playImpactEffectCallback));
            }
            else
            {
                if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
                    GameManager.Instance.PlaySFX(attack.soundEffectName);

                if (targetEnemyIndex >= 0 && targetEnemyIndex < enemies.Count && enemies[targetEnemyIndex] != null)
                {
                    playImpactEffectCallback(enemies[targetEnemyIndex], attack);
                }
                HandleDamageAttack(attack, targetEnemyIndex);
            }
            // Pass the correct delay to WaitForAnimationToComplete
            yield return monoBehaviourInstance.StartCoroutine(WaitForAnimationToComplete(animator, attack.animationTrigger, attack.flashInterval));
        }

        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (controllerWasTemporarilyChanged)
        {
            animator.runtimeAnimatorController = targetOriginalController;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private IEnumerator MoveToEnemyAndAttack(AttackData attack, int targetEnemyIndex, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= enemies.Count || enemies[targetEnemyIndex] == null || !enemies[targetEnemyIndex].activeInHierarchy)
        {
            QueueStatusUpdate("Invalid target!");
            yield break;
        }

        Vector3 enemyPos = enemies[targetEnemyIndex].transform.position;
        Vector3 targetPos = enemyPos + Vector3.left * attackDistance;

        yield return monoBehaviourInstance.StartCoroutine(MovePlayerTo(targetPos));

        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        // Use flashInterval for delay as it contains the correct timing value from the data
        yield return new WaitForSeconds(attack.flashInterval);

        if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
            GameManager.Instance.PlaySFX(attack.soundEffectName);

        if (targetEnemyIndex >= 0 && targetEnemyIndex < enemies.Count && enemies[targetEnemyIndex] != null)
        {
            playImpactEffectCallback(enemies[targetEnemyIndex], attack);
        }

        HandleDamageAttack(attack, targetEnemyIndex);

        // Pass the correct delay to WaitForAnimationToComplete
        yield return monoBehaviourInstance.StartCoroutine(WaitForAnimationToComplete(animator, attack.animationTrigger, attack.flashInterval));

        yield return monoBehaviourInstance.StartCoroutine(MovePlayerTo(originalPlayerPosition));
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

    private IEnumerator HandleAreaAttack(AttackData attack, Action<GameObject, AttackData> playImpactEffectCallback)
    {
        // FIX: Play sound for Area of Effect attacks
        if (GameManager.Instance != null && !string.IsNullOrEmpty(attack.soundEffectName) && attack.soundEffectName != "None")
        {
            GameManager.Instance.PlaySFX(attack.soundEffectName);
        }

        List<int> aliveEnemies = new List<int>();
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            if (currentEnemyHealths[i] > 0 && enemies[i].activeInHierarchy)
            {
                aliveEnemies.Add(i);
            }
        }

        var sbm = (StoryBattleManager)monoBehaviourInstance;
        float totalDamageDealt = 0f;
        QueueStatusUpdate("You used " + attack.attackName + " on all enemies!");

        foreach (int enemyIndex in aliveEnemies)
        {
            float initialHealth = currentEnemyHealths[enemyIndex];
            float finalDamage = (attack.damage + attack.damageIncrease) * getDamageMultiplier();
            bool isCrit = UnityEngine.Random.Range(0f, 1f) < (attack.critChance + getCritChanceBonus());

            if (isCrit)
            {
                finalDamage *= 2f;
                if (GameManager.Instance != null) GameManager.Instance.PlayCritSound();
            }

            currentEnemyHealths[enemyIndex] = Mathf.Max(0, currentEnemyHealths[enemyIndex] - finalDamage);
            float damageDone = initialHealth - currentEnemyHealths[enemyIndex];
            totalDamageDealt += damageDone;

            if (damageDone > 0)
            {
                QueueStatusUpdate("", sbm.uiManager.ShowEnemyHealthChange(enemyIndex, currentEnemyHealths[enemyIndex], -damageDone, isCrit));
            }

            if (enemyIndex < enemies.Count && enemies[enemyIndex] != null)
            {
                playImpactEffectCallback(enemies[enemyIndex], attack);
            }
        }

        yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());

        if (getHasLifesteal() && getLifestealPercentage() > 0)
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

        if (attack.doubleEdgeDamage > 0)
        {
            float reducedDamage = attack.doubleEdgeDamage * (1f - getDoubleEdgeReduction());

            float newPlayerHealth = Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage);
            setCurrentPlayerHealth(newPlayerHealth);
            QueueStatusUpdate("But you hurt yourself for " + Mathf.Round(reducedDamage) + " damage!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, -reducedDamage, false));
            yield return monoBehaviourInstance.StartCoroutine(WaitForQueue());
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

    private void HandleDamageAttack(AttackData attack, int targetEnemyIndex)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= currentEnemyHealths.Count || currentEnemyHealths[targetEnemyIndex] <= 0 || !enemies[targetEnemyIndex].activeInHierarchy)
        {
            return;
        }

        float finalDamage = (attack.damage + attack.damageIncrease) * getDamageMultiplier();
        float modifiedCritChance = Mathf.Min(1f, attack.critChance + getCritChanceBonus());
        bool isCrit = UnityEngine.Random.Range(0f, 1f) < modifiedCritChance;

        if (isCrit)
        {
            finalDamage *= 2f;
            if (GameManager.Instance != null)
                GameManager.Instance.PlayCritSound();
        }

        float initialHealth = currentEnemyHealths[targetEnemyIndex];
        currentEnemyHealths[targetEnemyIndex] = Mathf.Max(0, currentEnemyHealths[targetEnemyIndex] - finalDamage);
        float damageDone = initialHealth - currentEnemyHealths[targetEnemyIndex];

        var sbm = (StoryBattleManager)monoBehaviourInstance;
        Coroutine healthChangeAnim = (damageDone > 0) ? sbm.uiManager.ShowEnemyHealthChange(targetEnemyIndex, currentEnemyHealths[targetEnemyIndex], -damageDone, isCrit) : null;
        QueueStatusUpdate("You used " + attack.attackName + " on " + getEnemyNames()[targetEnemyIndex] + (isCrit ? "! It's a critical hit!" : "!"), healthChangeAnim);

        if (getHasLifesteal() && getLifestealPercentage() > 0)
        {
            float healAmount = finalDamage * (getLifestealPercentage() / 100f);
            if (healAmount > 0)
            {
                float newPlayerHealth = Mathf.Min(getPlayerMaxHealth(), getCurrentPlayerHealth() + healAmount);
                setCurrentPlayerHealth(newPlayerHealth);
                QueueStatusUpdate($"Lifesteal healed {Mathf.Round(healAmount)} HP!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, healAmount, false));
            }
        }

        if (attack.doubleEdgeDamage > 0)
        {
            float reducedDamage = attack.doubleEdgeDamage * (1f - getDoubleEdgeReduction());

            float newPlayerHealth = Mathf.Max(0, getCurrentPlayerHealth() - reducedDamage);
            setCurrentPlayerHealth(newPlayerHealth);
            QueueStatusUpdate("But you hurt yourself for " + Mathf.Round(reducedDamage) + " damage!", sbm.uiManager.ShowPlayerHealthChange(newPlayerHealth, -reducedDamage, false));
        }
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
                // Use flashInterval for delay as it contains the correct timing value from the data
                yield return new WaitForSeconds(attack.flashInterval);
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

        // Use flashInterval for delay as it contains the correct timing value from the data
        yield return new WaitForSeconds(attack.flashInterval);

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