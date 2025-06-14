﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for EventTrigger
using System; // Required for Action and Func

public class StoryBattleManager : MonoBehaviour
{
    [Header("Battle Setup")]
    [SerializeField] public GameObject Player;
    [SerializeField] public List<GameObject> Enemies = new List<GameObject>();
    [SerializeField] private Transform playerPosition;

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
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private GameObject attackHoverPrefab;
    [SerializeField] private GameObject enemyUIElementPrefab;
    [SerializeField] private GameObject attackEffectPrefab;

    [Header("Battle Settings")]
    [SerializeField] private float playerMaxHealth = 200f;
    [SerializeField] private float enemyMaxHealth = 30f;
    [SerializeField] private float hoverDelay = 0.5f;
    [SerializeField] private Vector3 enemyUIOffset = new Vector3(0, 1.5f, 0);

    [Header("Character Database")]
    public CharacterDatabase characterDB;

    private List<AttackData> playerAttacks = new List<AttackData>();

    public List<AttackData> GetCurrentPlayerAttacks()
    {
        return playerAttacks;
    }

    private Character selectedCharacter;
    private AttackData selectedAttack;
    private int selectedEnemyIndex = -1;
    private float currentPlayerHealth;
    private List<float> currentEnemyHealths = new List<float>();
    private GameObject currentHoverInstance;
    private Coroutine hoverCoroutine;

    // Player upgrade modifiers
    private float damageMultiplier = 1f;
    private float critChanceBonus = 0f;
    private float accuracyBonus = 0f;
    private float healingMultiplier = 1f;
    private float doubleEdgeReduction = 0f;
    public float LifestealPercentage { get; private set; } = 0f;
    public float ShieldAmount { get; private set; } = 0f;
    private float regenPerTurn = 0f;
    private bool hasLifesteal = false;
    private bool hasShield = false;
    private bool hasRegeneration = false;

    private Vector3 originalPlayerPosition;

    [Header("Wave System")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private float waveProgressionMultiplier = 1.1f;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDistance = 1.25f;

    [Header("Upgrade System")]
    [SerializeField] private UpgradeManager upgradeManager;

    internal UIManager uiManager;
    private AttackHandler attackHandler; // Declare here, but initialize later
    private EnemySpawner enemySpawner;
    private PlayerCharacter playerCharacter;

    private void Awake()
    {
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>().playOnAwake = false;
        }

        // Initialize parts that don't depend on player character setup yet
        uiManager = new UIManager(this, battlePanel, attackPanel, enemySelectionPanel, endScreenPanel, enemyUIPanel,
                                   playerHealthText, battleStatusText, resultText, returnToMenuButton, continueButton,
                                   enemySelectionPanel.transform,
                                   attackButtonPrefab, attackHoverPrefab, enemyUIElementPrefab,
                                   hoverDelay, enemyUIOffset, waveReachedText, () => enemySpawner.GetEnemyNames());

        enemySpawner = new EnemySpawner(Enemies, enemyPrefabs, enemySpawnPoints);
        playerCharacter = new PlayerCharacter(Player, playerPosition); // PlayerCharacter object created.
    }

    private void Start()
    {
        // Now that UIManager, EnemySpawner, and PlayerCharacter objects exist,
        // we can perform the full battle initialization which sets up the character and then AttackHandler.
        InitializeStoryBattle();
    }

    private void InitializeStoryBattle()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt("selectedStoryCharacter", 0);

        if (characterDB != null)
        {
            selectedCharacter = characterDB.GetCharacter(selectedCharacterIndex);

            // CRUCIAL: Set the player character's initial animator controller FIRST.
            // This happens once at the start of the battle mode.
            playerCharacter.SetCharacter(selectedCharacter);

            playerAttacks.Clear();
            playerAttacks.Add(StoryAttackDataManager.Instance.GetStartingAttackForCharacter(selectedCharacter.characterName));
        }
        else
        {
            Debug.LogError("Could not find CharacterDatabase!");
            return;
        }

        // Now that playerCharacter's Animator is set, initialize AttackHandler
        // The AttackHandler constructor will now correctly grab the _playerOriginalAnimatorController.
        attackHandler = new AttackHandler(this, Player, Enemies, currentEnemyHealths,
                                          () => enemySpawner.GetEnemyNames(),
                                          () => currentPlayerHealth, (newHealth) => currentPlayerHealth = newHealth,
                                          () => playerMaxHealth,
                                          () => damageMultiplier, () => critChanceBonus, () => accuracyBonus,
                                          () => healingMultiplier, () => doubleEdgeReduction,
                                          () => hasLifesteal, () => LifestealPercentage,
                                          () => hasShield, (amount) => ShieldAmount = amount,
                                          moveSpeed, attackDistance, originalPlayerPosition, attackEffectPrefab);

        currentPlayerHealth = playerMaxHealth;
        currentWave = 1;
        ResetUpgradeModifiers();

        enemySpawner.SpawnWaveEnemies(currentWave, waveProgressionMultiplier, enemyMaxHealth, currentEnemyHealths);
        uiManager.CreateEnemyUI(Enemies, currentEnemyHealths);
        playerCharacter.SetupPlayer(); // This handles position, etc.
        originalPlayerPosition = playerCharacter.OriginalPlayerPosition;

        uiManager.SetEnemySelectionPanelActive(false);
        UpdateHealthDisplays();
        uiManager.UpdateBattleStatus($"Wave {currentWave} - Choose your attack!");
        ShowAttackOptions();
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

        if (upgrade.damageMultiplier != 1f)
        {
            damageMultiplier *= Mathf.Pow(upgrade.damageMultiplier, stacks);
        }

        if (upgrade.critChanceIncrease > 0)
        {
            critChanceBonus += upgrade.critChanceIncrease * stacks;
        }

        if (upgrade.accuracyIncrease > 0)
        {
            accuracyBonus += upgrade.accuracyIncrease * stacks;
        }

        if (upgrade.healingMultiplier != 1f)
        {
            healingMultiplier *= Mathf.Pow(upgrade.healingMultiplier, stacks);
        }

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
            if (newAttack != null && !playerAttacks.Contains(newAttack))
            {
                playerAttacks.Add(newAttack);
                ShowAttackOptions(); // Re-display attack buttons to show the new attack
            }
        }
        UpdateHealthDisplays();
    }

    private void UpdateHealthDisplays()
    {
        uiManager.UpdatePlayerHealth(currentPlayerHealth);
        uiManager.UpdateEnemyHealths(currentEnemyHealths);
    }

    private void OnAttackSelected(AttackData attack)
    {
        selectedAttack = attack;

        if (attack.attackType == AttackType.Heal)
        {
            StartCoroutine(PerformAttackCoroutine(attack, -1));
            return;
        }
        else if (attack.attackType == AttackType.AreaEffect)
        {
            StartCoroutine(PerformAttackCoroutine(attack, -1));
            return;
        }

        List<int> aliveEnemies = GetAliveEnemyIndices();

        if (aliveEnemies.Count == 0)
        {
            CheckForBattleEnd();
            return;
        }
        else if (aliveEnemies.Count == 1)
        {
            StartCoroutine(PerformAttackCoroutine(attack, aliveEnemies[0]));
            return;
        }

        ShowEnemySelection();
    }

    private List<int> GetAliveEnemyIndices()
    {
        List<int> aliveIndices = new List<int>();
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            if (currentEnemyHealths[i] > 0 && Enemies[i] != null && Enemies[i].activeInHierarchy)
            {
                aliveIndices.Add(i);
            }
        }
        return aliveIndices;
    }

    private void ShowEnemySelection()
    {
        uiManager.SetAttackPanelActive(false);
        uiManager.OnAttackButtonHoverExit(ref hoverCoroutine, ref currentHoverInstance);
        uiManager.SetEnemySelectionPanelActive(true);
        uiManager.UpdateBattleStatus("Select target enemy for " + selectedAttack.attackName + "!");

        List<int> aliveEnemies = GetAliveEnemyIndices();
        uiManager.SetupEnemySelectionButtons(aliveEnemies, enemySpawner.GetEnemyNames(), OnEnemySelected);
    }

    private void OnEnemySelected(int enemyIndex)
    {
        selectedEnemyIndex = enemyIndex;
        uiManager.SetEnemySelectionPanelActive(false);
        uiManager.SetAttackPanelActive(true);
        StartCoroutine(PerformAttackCoroutine(selectedAttack, selectedEnemyIndex));
    }

    private IEnumerator PerformAttackCoroutine(AttackData attack, int targetEnemyIndex)
    {
        SetAttackButtonsInteractable(false);

        // Cooldown check and application removed from AttackHandler.PerformAttack.
        yield return attackHandler.PerformAttack(attack, targetEnemyIndex,
            uiManager.UpdateBattleStatus,
            uiManager.PlayImpactEffect,
            () => uiManager.FlashSprite(Player.GetComponent<SpriteRenderer>(), attack));

        UpdateHealthDisplays();
        CheckForBattleEnd();
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
                UpdateHealthDisplays();
            }
        }
    }

    private void CheckForBattleEnd()
    {
        if (currentPlayerHealth <= 0)
        {
            StartCoroutine(HandlePlayerDefeat());
        }
        else if (GetAliveEnemyIndices().Count == 0)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (currentEnemyHealths[i] <= 0 && Enemies[i] != null)
                {
                    Enemies[i].SetActive(false);
                }
            }
            StartCoroutine(HandleAllEnemiesDefeated());
        }
        else
        {
            ApplyTurnStartEffects();
            // Removed: attackHandler.DecrementAttackCooldowns(playerAttacks); // Cooldowns are removed
            ShowAttackOptions(); // Re-display attacks (no cooldown update needed on buttons now)
            SetAttackButtonsInteractable(true);
            uiManager.UpdateBattleStatus("Choose your next attack!");
        }
    }

    private IEnumerator HideEnemyAfterDeath(int enemyIndex)
    {
        Animator enemyAnimator = Enemies[enemyIndex].GetComponent<Animator>();
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("Death");
            yield return new WaitForSeconds(enemyAnimator.GetCurrentAnimatorStateInfo(0).length);
        }

        yield return new WaitForSeconds(0.5f);
        if (Enemies[enemyIndex] != null)
        {
            Enemies[enemyIndex].SetActive(false);
        }
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

        foreach (GameObject enemy in Enemies)
        {
            if (enemy != null)
            {
                Animator enemyAnimator = enemy.GetComponent<Animator>();
                if (enemyAnimator != null)
                {
                    enemyAnimator.SetTrigger("Death");
                }
            }
        }

        yield return new WaitForSeconds(2f);

        for (int i = 0; i < Enemies.Count; i++)
        {
            if (currentEnemyHealths[i] <= 0 && Enemies[i] != null)
            {
                Enemies[i].SetActive(false);
            }
        }

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

    private void StartNextWave()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradeSelectionComplete -= StartNextWave;
        }

        currentWave++;
        uiManager.UpdateBattleStatus($"Preparing Wave {currentWave}...");

        StartCoroutine(WaveTransition());
    }

    private IEnumerator WaveTransition()
    {
        yield return new WaitForSeconds(0.5f);

        ResetEnemiesForNewWave();

        UpdateHealthDisplays();
        uiManager.UpdateBattleStatus($"Wave {currentWave} - Choose your attack!");
        SetAttackButtonsInteractable(true);
    }

    private void ResetEnemiesForNewWave()
    {
        uiManager.ClearEnemyUI();
        enemySpawner.SpawnWaveEnemies(currentWave, waveProgressionMultiplier, enemyMaxHealth, currentEnemyHealths);
        uiManager.CreateEnemyUI(Enemies, currentEnemyHealths);
    }

    private void SetAttackButtonsInteractable(bool interactable)
    {
        uiManager.SetAttackButtonsInteractable(interactable);
    }

    private void ShowAttackOptions()
    {
        uiManager.ClearAttackButtons();
        // Pass playerAttacks directly. UIManager no longer needs to handle disabling buttons based on cooldown.
        uiManager.CreateAttackButtons(playerAttacks, OnAttackSelected, OnAttackButtonHoverEnter, OnAttackButtonHoverExit);
    }

    private void OnAttackButtonHoverEnter(AttackData attack, RectTransform buttonRect)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(uiManager.ShowHoverAfterDelay(attack, buttonRect, (hoverObj) => currentHoverInstance = hoverObj));
    }

    private void OnAttackButtonHoverExit()
    {
        uiManager.OnAttackButtonHoverExit(ref hoverCoroutine, ref currentHoverInstance);
    }
}