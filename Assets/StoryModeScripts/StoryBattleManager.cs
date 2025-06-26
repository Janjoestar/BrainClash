using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryBattleManager : MonoBehaviour
{
    public enum BattleState
    {
        PlayerTurn,
        EnemyTurn,
        SelectingAttackForUpgrade,
        Busy
    }
    private BattleState currentState;

    [Header("Battle Setup")]
    [SerializeField] public GameObject Player;
    [SerializeField] public List<GameObject> Enemies = new List<GameObject>();
    [SerializeField] private Transform playerPosition;
    private HashSet<int> processedAsDefeated = new HashSet<int>();

    [Header("UI Panels")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject enemySelectionPanel;
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private GameObject enemyUIPanel;

    [Header("UI Elements")]
    [SerializeField] private Text playerHealthText;
    [SerializeField] private Text battleStatusText;
    [SerializeField] private Text resultText;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Text waveReachedText;

    [Header("Prefabs")]
    [SerializeField] public GameObject attackButtonPrefab;
    [SerializeField] private GameObject attackHoverPrefab;
    [SerializeField] private GameObject enemyUIElementPrefab;
    [SerializeField] private GameObject attackEffectPrefab;

    [Header("Battle Settings")]
    [SerializeField] public float playerMaxHealth = 200f;
    [SerializeField] public float enemyMaxHealth = 30f;
    [SerializeField] private float hoverDelay = 0.5f;
    [SerializeField] private Vector3 enemyUIOffset = new Vector3(0, 1.5f, 0);

    [Header("Character Database")]
    public CharacterDatabase characterDB;

    [Header("AI Systems")]
    [SerializeField] private GroqAI_Handler groqHandler;

    [Header("Wave System")]
    [SerializeField] public int currentWave = 1;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] public float waveProgressionMultiplier = 1.1f;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDistance = 1.25f;

    [Header("System References")]
    [SerializeField] public UpgradeManager upgradeManager;
    public UIManager uiManager;
    public AttackHandler attackHandler;
    public EnemySpawner enemySpawner;
    public PlayerCharacter playerCharacter;

    // Battle State
    private bool isPlayerTurn = false;
    private List<AttackData> playerAttacks = new List<AttackData>();
    public Dictionary<StatusEffectType, StatusEffect> playerStatusEffects = new Dictionary<StatusEffectType, StatusEffect>();
    private UpgradeData pendingAttackModification;
    private Character selectedCharacter;
    private AttackData selectedAttack;
    private int selectedEnemyIndex = -1;
    public float currentPlayerHealth;
    public List<float> currentEnemyHealths = new List<float>();
    private GameObject currentHoverInstance;
    private Coroutine hoverCoroutine;
    private Vector3 originalPlayerPosition;
    private Queue<string> battleStatusQueue = new Queue<string>();
    private bool isDisplayingStatus = false;

    // Upgrade-related Modifiers
    public float damageMultiplier = 1f;
    public float critChanceBonus = 0f;
    public float accuracyBonus = 0f;
    public float healingMultiplier = 1f;
    public float doubleEdgeReduction = 0f;
    public float LifestealPercentage { get; private set; } = 0f;
    public float ShieldAmount { get; private set; } = 0f;
    public float regenPerTurn = 0f;
    public bool hasLifesteal = false;
    public bool hasShield = false;
    public bool hasRegeneration = false;

    #region Unity Lifecycle
    private void Awake()
    {
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>().playOnAwake = false;
        }

        Enemies.Clear();

        uiManager = new UIManager(
            this, battlePanel, attackPanel, enemySelectionPanel, endScreenPanel, enemyUIPanel,
            playerHealthText, battleStatusText, resultText, returnToMenuButton, continueButton,
            enemySelectionPanel.transform, attackButtonPrefab, attackHoverPrefab,
            enemyUIElementPrefab, hoverDelay, enemyUIOffset, waveReachedText,
            () => enemySpawner.GetEnemyNames()
        );

        enemySpawner = new EnemySpawner(Enemies, enemyPrefabs, enemySpawnPoints);
        playerCharacter = new PlayerCharacter(Player, playerPosition);
    }

    private void Start()
    {
        InitializeStoryBattle();
    }
    #endregion

    #region Initialization
    private void InitializeStoryBattle()
    {
        processedAsDefeated.Clear();
        int selectedCharacterIndex = PlayerPrefs.GetInt("selectedStoryCharacter", 0);
        bool unlockAll = PlayerPrefs.GetInt("unlockAllAbilitiesDebug", 0) == 1;

        if (characterDB == null) return;

        selectedCharacter = characterDB.GetCharacter(selectedCharacterIndex);
        playerCharacter.SetCharacter(selectedCharacter);
        playerAttacks.Clear();

        var attackManager = StoryAttackDataManager.Instance;
        if (unlockAll)
        {
            playerAttacks.AddRange(attackManager.GetAttacksForCharacter(selectedCharacter.characterName));
        }
        else
        {
            playerAttacks.Add(attackManager.GetStartingAttackForCharacter(selectedCharacter.characterName));
        }

        playerCharacter.SetupPlayer();
        originalPlayerPosition = playerCharacter.OriginalPlayerPosition;

        attackHandler = new AttackHandler(
            this, Player, Enemies, currentEnemyHealths, () => enemySpawner.GetEnemyNames(),
            () => currentPlayerHealth, (newHealth) => currentPlayerHealth = newHealth,
            () => playerMaxHealth, GetPlayerDamageMultiplier, () => critChanceBonus,
            GetPlayerAccuracyBonus, () => healingMultiplier, () => doubleEdgeReduction,
            () => hasLifesteal, () => LifestealPercentage, () => hasShield,
            (amount) => ShieldAmount = amount, playerStatusEffects, QueueBattleStatus,
            moveSpeed, attackDistance, originalPlayerPosition, attackEffectPrefab
        );

        currentPlayerHealth = playerMaxHealth;
        currentWave = 1;
        ResetUpgradeModifiers();

        enemySpawner.SpawnWaveEnemies(currentWave, waveProgressionMultiplier, enemyMaxHealth, currentEnemyHealths);
        uiManager.CreateEnemyUI(Enemies, currentEnemyHealths);
        uiManager.SetEnemySelectionPanelActive(false);

        UpdateHealthDisplays();
        StartPlayerTurn();
    }
    #endregion

    #region Public API & State Management
    public List<AttackData> GetCurrentPlayerAttacks() => new List<AttackData>(playerAttacks);
    public AttackData GetPlayerAttackInstance(string attackName) => playerAttacks.FirstOrDefault(a => a.attackName == attackName);

    public void EnterAttackSelectionModeForUpgrade(UpgradeData upgrade)
    {
        pendingAttackModification = upgrade;
        currentState = BattleState.SelectingAttackForUpgrade;
        uiManager.UpdateBattleStatus($"Apply '{upgrade.upgradeName}' to which attack?");
        ShowAttackOptions();
        SetAttackButtonsInteractable(true);
    }

    public void ApplyUpgrade(UpgradeData upgrade, int stacks)
    {
        if (upgrade.healthIncrease > 0)
        {
            float healthIncrease = upgrade.healthIncrease * stacks;
            playerMaxHealth += healthIncrease;
            currentPlayerHealth += healthIncrease;
            currentPlayerHealth = Mathf.Min(currentPlayerHealth, playerMaxHealth);
        }
        if (upgrade.damageMultiplier != 1f) damageMultiplier *= Mathf.Pow(upgrade.damageMultiplier, stacks);
        if (upgrade.critChanceIncrease > 0) critChanceBonus += upgrade.critChanceIncrease * stacks;
        if (upgrade.accuracyIncrease > 0) accuracyBonus += upgrade.accuracyIncrease * stacks;
        if (upgrade.healingMultiplier != 1f) healingMultiplier *= Mathf.Pow(upgrade.healingMultiplier, stacks);
        if (upgrade.doubleEdgeReduction > 0)
        {
            doubleEdgeReduction += upgrade.doubleEdgeReduction * stacks;
            doubleEdgeReduction = Mathf.Min(doubleEdgeReduction, 1f);
        }
        if (upgrade.grantsLifesteal)
        {
            hasLifesteal = true;
            LifestealPercentage += upgrade.lifestealPercentage * stacks;
        }
        if (upgrade.grantsShield)
        {
            hasShield = true;
            ShieldAmount += upgrade.shieldAmount * stacks;
        }
        if (upgrade.grantsRegeneration)
        {
            hasRegeneration = true;
            regenPerTurn += upgrade.regenPerTurn * stacks;
        }
        if (upgrade.grantsNewAttack && !string.IsNullOrEmpty(upgrade.newAttackName))
        {
            AttackData newAttack = StoryAttackDataManager.Instance.GetAttackByName(upgrade.newAttackName);
            if (newAttack != null && !playerAttacks.Any(a => a.attackName == newAttack.attackName))
            {
                playerAttacks.Add(newAttack);
                ShowAttackOptions();
            }
        }
        UpdateHealthDisplays();
    }
    #endregion

    #region Turn and Battle Flow
    private void StartPlayerTurn()
    {
        currentState = BattleState.PlayerTurn;
        isPlayerTurn = true;
        ApplyTurnStartEffects();
        uiManager.UpdateBattleStatus("Choose your next attack!");
        SetAttackButtonsInteractable(true);
        ShowAttackOptions();
    }

    private void OnAttackSelected(AttackData attack)
    {
        HideAttackHover();

        switch (currentState)
        {
            case BattleState.PlayerTurn:
                selectedAttack = attack;

                // Self-targeting or full AoE attacks don't need target selection.
                if (attack.attackType == AttackType.Heal || attack.numberOfTargets >= 99)
                {
                    StartCoroutine(PerformAttackCoroutine(attack, -1));
                    return;
                }

                List<int> aliveEnemies = GetAliveEnemyIndices();
                if (aliveEnemies.Count == 0) CheckForBattleEnd();
                else if (aliveEnemies.Count == 1) StartCoroutine(PerformAttackCoroutine(attack, aliveEnemies[0]));
                else ShowEnemySelection();
                break;

            case BattleState.SelectingAttackForUpgrade:
                ApplyPendingModificationToAttack(attack);
                break;
        }
    }

    private void CheckForBattleEnd()
    {
        currentState = BattleState.Busy;

        if (currentPlayerHealth <= 0)
        {
            StartCoroutine(HandlePlayerDefeat());
            return;
        }

        if (GetAliveEnemyIndices().Count == 0)
        {
            StartCoroutine(HandleAllEnemiesDefeated());
            return;
        }

        // Transition to the next turn
        if (isPlayerTurn)
        {
            isPlayerTurn = false;
            currentState = BattleState.EnemyTurn;
            StartCoroutine(EnemiesTurnCoroutine());
        }
        else
        {
            StartPlayerTurn();
        }
    }
    #endregion

    #region Coroutines
    private IEnumerator PerformAttackCoroutine(AttackData attack, int targetEnemyIndex)
    {
        currentState = BattleState.Busy;
        SetAttackButtonsInteractable(false);

        yield return attackHandler.PerformAttack(
            attack,
            targetEnemyIndex,
            uiManager.PlayImpactEffect,
            () => uiManager.FlashSprite(Player.GetComponent<SpriteRenderer>(), attack)
        );

        UpdateHealthDisplays();

        var deathRoutines = new List<Coroutine>();
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (currentEnemyHealths[i] <= 0 && !processedAsDefeated.Contains(i))
            {
                processedAsDefeated.Add(i);
                deathRoutines.Add(StartCoroutine(IndividualEnemyDeath(Enemies[i], i)));
            }
        }
        foreach (var routine in deathRoutines) yield return routine;

        CheckForBattleEnd();
    }

    private IEnumerator EnemiesTurnCoroutine()
    {
        uiManager.UpdateBattleStatus("Enemy Turn!");
        SetAttackButtonsInteractable(false);
        yield return new WaitForSeconds(0.5f);

        List<int> aliveEnemies = GetAliveEnemyIndices();
        foreach (int enemyIndex in aliveEnemies)
        {
            if (currentPlayerHealth <= 0) break;

            GameObject currentEnemy = Enemies[enemyIndex];
            EnemyAI enemyAI = currentEnemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.DecrementCooldowns();
                uiManager.UpdateBattleStatus($"{currentEnemy.name.Replace("(Clone)", "")} is thinking...");

                var choiceResult = new AttackChoiceResult();
                yield return StartCoroutine(enemyAI.GetSmartAttack(this, groqHandler, choiceResult));
                AttackData chosenAttack = choiceResult.ChosenAttack;

                if (chosenAttack != null)
                {
                    yield return attackHandler.PerformEnemyAttack(
                        chosenAttack,
                        currentEnemy,
                        uiManager.PlayImpactEffect,
                        () => uiManager.FlashSprite(Player.GetComponent<SpriteRenderer>(), chosenAttack)
                    );
                    enemyAI.SetCooldownOnAttack(chosenAttack.attackName);
                }
                else
                {
                    uiManager.UpdateBattleStatus($"{currentEnemy.name.Replace("(Clone)", "")} has no available attacks!");
                    yield return new WaitForSeconds(1.0f);
                }
                UpdateHealthDisplays();
                if (currentPlayerHealth <= 0) break;
                yield return new WaitForSeconds(0.5f);
            }
        }
        CheckForBattleEnd();
    }

    private IEnumerator IndividualEnemyDeath(GameObject enemy, int enemyIndex)
    {
        if (enemy == null) yield break;
        Animator animator = enemy.GetComponent<Animator>();
        if (animator == null) yield break;

        animator.SetTrigger("Death");
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        enemy.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        if (uiManager != null) uiManager.DeactivateEnemyUIElement(enemyIndex);
    }

    private IEnumerator HandlePlayerDefeat()
    {
        uiManager.UpdateBattleStatus("You have been defeated!");
        Animator playerAnimator = Player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Death");
            yield return new WaitForSeconds(2f);
        }
        uiManager.ShowEndScreen(false, currentWave, maxWaves);
        uiManager.SetupEndScreenButtons(false, () => SceneManager.LoadScene("StartScreen"), () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    private IEnumerator HandleAllEnemiesDefeated()
    {
        uiManager.UpdateBattleStatus($"Wave {currentWave} cleared!");
        yield return new WaitForSeconds(1f);

        if (currentWave < maxWaves)
        {
            if (upgradeManager != null)
            {
                upgradeManager.OnUpgradeSelectionComplete += StartNextWave;
                upgradeManager.ShowUpgradeSelection(currentWave);
            }
            else
            {
                StartNextWave();
            }
        }
        else
        {
            uiManager.ShowEndScreen(true, currentWave, maxWaves);
            uiManager.SetupEndScreenButtons(true, () => SceneManager.LoadScene("StartScreen"), () => SceneManager.LoadScene("StoryMode"));
        }
    }

    private IEnumerator WaveTransition()
    {
        yield return new WaitForSeconds(0.5f);
        ResetEnemiesForNewWave();
        UpdateHealthDisplays();
        StartPlayerTurn();
    }

    private IEnumerator ProcessStatusQueue()
    {
        isDisplayingStatus = true;
        while (battleStatusQueue.Count > 0)
        {
            uiManager.UpdateBattleStatus(battleStatusQueue.Dequeue());
            yield return new WaitForSeconds(1.25f);
        }
        isDisplayingStatus = false;
    }
    #endregion

    #region UI & Event Handlers
    private void OnEnemySelected(int enemyIndex)
    {
        OnEnemyButtonHoverExit(enemyIndex);
        selectedEnemyIndex = enemyIndex;
        uiManager.SetEnemySelectionPanelActive(false);
        uiManager.SetAttackPanelActive(true);
        StartCoroutine(PerformAttackCoroutine(selectedAttack, selectedEnemyIndex));
    }

    private void OnAttackButtonHoverEnter(AttackData attack, RectTransform buttonRect)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(uiManager.ShowHoverAfterDelay(attack, buttonRect, (hoverObj) => currentHoverInstance = hoverObj));
        HighlightPotentialTargets(attack);
    }

    private void OnAttackButtonHoverExit() => uiManager.OnAttackButtonHoverExit(ref hoverCoroutine, ref currentHoverInstance);

    public void OnEnemyButtonHoverEnter(int enemyIndex)
    {
        SpriteRenderer renderer = Enemies.ElementAtOrDefault(enemyIndex)?.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = new Color(1f, 1f, 0.7f);
    }

    public void OnEnemyButtonHoverExit(int enemyIndex)
    {
        SpriteRenderer renderer = Enemies.ElementAtOrDefault(enemyIndex)?.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = Color.white;
    }

    public void ShowAttackOptions() => uiManager.CreateAttackButtons(playerAttacks, OnAttackSelected, OnAttackButtonHoverEnter, OnAttackButtonHoverExit);

    private void ShowEnemySelection()
    {
        uiManager.SetAttackPanelActive(false);
        uiManager.SetEnemySelectionPanelActive(true);
        uiManager.UpdateBattleStatus("Select target enemy for " + selectedAttack.attackName + "!");
        List<int> aliveEnemies = GetAliveEnemyIndices();
        uiManager.SetupEnemySelectionButtons(aliveEnemies, enemySpawner.GetEnemyNames(), OnEnemySelected);
    }

    private void UpdateHealthDisplays()
    {
        uiManager.UpdatePlayerHealth(currentPlayerHealth);
        uiManager.UpdateEnemyHealths(currentEnemyHealths);
    }

    public void QueueBattleStatus(string message)
    {
        if (!isDisplayingStatus) StartCoroutine(ProcessStatusQueue());
        battleStatusQueue.Enqueue(message);
    }
    #endregion

    #region Helper Methods
    private void ApplyPendingModificationToAttack(AttackData attackToModify)
    {
        if (pendingAttackModification == null) return;

        // Get the actual instance from the player's list to modify it
        AttackData attackInstance = GetPlayerAttackInstance(attackToModify.attackName);
        if (attackInstance == null)
        {
            Debug.LogError($"Could not find attack instance named {attackToModify.attackName} to upgrade.");
            return;
        }

        switch (pendingAttackModification.attackModificationType)
        {
            case AttackModificationType.AddTargets:
                attackInstance.numberOfTargets += pendingAttackModification.addTargets;
                break;
            case AttackModificationType.SetToFullAOE:
                attackInstance.numberOfTargets = 99;
                break;
        }
        attackInstance.damageMultiplier *= pendingAttackModification.attackDamageMultiplier;

        uiManager.UpdateBattleStatus($"'{attackInstance.attackName}' was empowered!");
        currentState = BattleState.Busy;
        SetAttackButtonsInteractable(false);

        upgradeManager.FinalizeUpgradeSelection(pendingAttackModification);
        pendingAttackModification = null;
    }

    private void ApplyTurnStartEffects()
    {
        if (hasRegeneration && regenPerTurn > 0)
        {
            float healAmount = Mathf.Min(regenPerTurn, playerMaxHealth - currentPlayerHealth);
            if (healAmount > 0)
            {
                currentPlayerHealth += healAmount;
                uiManager.UpdateBattleStatus($"Regeneration healed {Mathf.Round(healAmount)} HP!");
                uiManager.ShowPlayerHealthChange(currentPlayerHealth, healAmount);
            }
        }

        if (playerStatusEffects.Count > 0)
        {
            var effectsToRemove = new List<StatusEffectType>();
            var keys = new List<StatusEffectType>(playerStatusEffects.Keys);
            foreach (var key in keys)
            {
                playerStatusEffects[key].duration--;
                if (playerStatusEffects[key].duration <= 0)
                {
                    effectsToRemove.Add(key);
                    uiManager.UpdateBattleStatus($"{key} has worn off.");
                }
            }
            foreach (var key in effectsToRemove) playerStatusEffects.Remove(key);
        }
    }

    private void StartNextWave()
    {
        if (upgradeManager != null) upgradeManager.OnUpgradeSelectionComplete -= StartNextWave;
        currentWave++;
        uiManager.UpdateBattleStatus($"Preparing Wave {currentWave}...");
        StartCoroutine(WaveTransition());
    }

    private void ResetEnemiesForNewWave()
    {
        processedAsDefeated.Clear();
        uiManager.ClearEnemyUI();
        enemySpawner.SpawnWaveEnemies(currentWave, waveProgressionMultiplier, enemyMaxHealth, currentEnemyHealths);
        uiManager.CreateEnemyUI(Enemies, currentEnemyHealths);
    }

    private void ResetUpgradeModifiers()
    {
        damageMultiplier = 1f;
        critChanceBonus = 0f;
        accuracyBonus = 0f;
        healingMultiplier = 1f;
        doubleEdgeReduction = 0f;
        LifestealPercentage = 0f;
        ShieldAmount = 0f;
        regenPerTurn = 0f;
        hasLifesteal = false;
        hasShield = false;
        hasRegeneration = false;
        playerStatusEffects.Clear();
    }

    private void SetAttackButtonsInteractable(bool interactable) => uiManager.SetAttackButtonsInteractable(interactable);
    public List<int> GetAliveEnemyIndices()
    {
        var aliveIndices = new List<int>();
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            if (currentEnemyHealths[i] > 0 && Enemies[i] != null && Enemies[i].activeInHierarchy)
            {
                aliveIndices.Add(i);
            }
        }
        return aliveIndices;
    }

    public float GetPlayerDamageMultiplier()
    {
        float multiplier = damageMultiplier;
        if (playerStatusEffects.ContainsKey(StatusEffectType.DamageDown))
        {
            multiplier *= (1 - playerStatusEffects[StatusEffectType.DamageDown].value);
        }
        return multiplier;
    }

    public float GetPlayerAccuracyBonus()
    {
        float bonus = accuracyBonus;
        if (playerStatusEffects.ContainsKey(StatusEffectType.AccuracyDown))
        {
            bonus -= playerStatusEffects[StatusEffectType.AccuracyDown].value;
        }
        return bonus;
    }

    public void HighlightPotentialTargets(AttackData attack)
    {
        ClearAllEnemyHighlights();
        List<int> aliveEnemies = GetAliveEnemyIndices();
        if (aliveEnemies.Count == 0) return;

        Color highlightColor = Color.yellow;
        if (attack.numberOfTargets < aliveEnemies.Count && attack.numberOfTargets > 1)
        {
            highlightColor = new Color(0.7f, 0.9f, 1f); // Light blue for multi-target but not all
        }

        if (attack.numberOfTargets > 1)
        {
            HighlightEnemies(aliveEnemies, highlightColor);
        }
    }

    public void HighlightEnemies(List<int> indices, Color color)
    {
        foreach (int index in indices)
        {
            if (Enemies.Count > index && Enemies[index] != null)
            {
                Enemies[index].GetComponent<SpriteRenderer>().color = color;
            }
        }
    }

    public void ClearAllEnemyHighlights()
    {
        foreach (GameObject enemy in Enemies)
        {
            if (enemy != null)
            {
                enemy.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    public void HideAttackHover() => uiManager.OnAttackButtonHoverExit(ref hoverCoroutine, ref currentHoverInstance);
    #endregion
}