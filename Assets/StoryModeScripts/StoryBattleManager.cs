using System.Collections;
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
    [SerializeField] private GameObject attackButtonPrefab;
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


    private List<AttackData> playerAttacks = new List<AttackData>();

    public Dictionary<StatusEffectType, StatusEffect> playerStatusEffects = new Dictionary<StatusEffectType, StatusEffect>();

    public List<AttackData> GetCurrentPlayerAttacks()
    {
        return playerAttacks;
    }

    private Character selectedCharacter;
    private AttackData selectedAttack;
    private int selectedEnemyIndex = -1;
    public float currentPlayerHealth;
    public List<float> currentEnemyHealths = new List<float>();
    private GameObject currentHoverInstance;
    private Coroutine hoverCoroutine;

    // Player upgrade modifiers
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

    private Vector3 originalPlayerPosition;

    [Header("Wave System")]
    [SerializeField] public int currentWave = 1;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] public float waveProgressionMultiplier = 1.1f;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDistance = 1.25f;

    [Header("Upgrade System")]
    [SerializeField] public UpgradeManager upgradeManager;

    public UIManager uiManager;
    public AttackHandler attackHandler; // Declare here, but initialize later
    public EnemySpawner enemySpawner;
    public PlayerCharacter playerCharacter;

    private Queue<string> battleStatusQueue = new Queue<string>();
    private bool isDisplayingStatus = false;

    // Add this new method to queue messages
    public void QueueBattleStatus(string message)
    {
        battleStatusQueue.Enqueue(message);
        if (!isDisplayingStatus)
        {
            StartCoroutine(ProcessStatusQueue());
        }
    }

    // Add this new coroutine to process the queue
    private IEnumerator ProcessStatusQueue()
    {
        isDisplayingStatus = true;
        while (battleStatusQueue.Count > 0)
        {
            uiManager.UpdateBattleStatus(battleStatusQueue.Dequeue());
            yield return new WaitForSeconds(1.25f); // How long each message stays on screen
        }
        isDisplayingStatus = false;
    }

    private void Awake()
    {
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>().playOnAwake = false;
        }

        Enemies.Clear();

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
        processedAsDefeated.Clear();
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

        // --- BUG FIX: INITIALIZATION ORDER ---
        // Setup player position and store it FIRST.
        playerCharacter.SetupPlayer();
        originalPlayerPosition = playerCharacter.OriginalPlayerPosition;

        // NOW initialize AttackHandler with the correct originalPlayerPosition.
        attackHandler = new AttackHandler(this, Player, Enemies, currentEnemyHealths,
    () => enemySpawner.GetEnemyNames(),
    () => currentPlayerHealth, (newHealth) => currentPlayerHealth = newHealth,
    () => playerMaxHealth,
    GetPlayerDamageMultiplier,
    () => critChanceBonus,
    GetPlayerAccuracyBonus,
    () => healingMultiplier, () => doubleEdgeReduction,
    () => hasLifesteal, () => LifestealPercentage,
    () => hasShield, (amount) => ShieldAmount = amount,
    playerStatusEffects,
    QueueBattleStatus, // <-- CHANGE THIS LINE
    moveSpeed, attackDistance, originalPlayerPosition, attackEffectPrefab);

        currentPlayerHealth = playerMaxHealth;
        currentWave = 1;
        ResetUpgradeModifiers();

        enemySpawner.SpawnWaveEnemies(currentWave, waveProgressionMultiplier, enemyMaxHealth, currentEnemyHealths);
        uiManager.CreateEnemyUI(Enemies, currentEnemyHealths);
        // playerCharacter.SetupPlayer(); // This is now done earlier

        uiManager.SetEnemySelectionPanelActive(false);
        UpdateHealthDisplays();
        uiManager.UpdateBattleStatus($"Wave {currentWave} - Choose your attack!");
        ShowAttackOptions();
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

    private void ApplyTurnStartEffects()
    {
        if (hasRegeneration && regenPerTurn > 0)
        {
            float healAmount = Mathf.Min(regenPerTurn, playerMaxHealth - currentPlayerHealth);
            if (healAmount > 0)
            {
                currentPlayerHealth += healAmount;
                uiManager.UpdateBattleStatus($"Regeneration healed {Mathf.Round(healAmount)} HP!");
                // --- FIX: Call the renamed animation method ---
                uiManager.ShowPlayerHealthChange(currentPlayerHealth, healAmount);
            }
        }

        // Apply and tick down status effects
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
            foreach (var key in effectsToRemove)
            {
                playerStatusEffects.Remove(key);
            }
        }
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
        // --- BUG FIX: PERSISTENT HOVER ---
        // Hide the hover UI as soon as a button is clicked.
        HideAttackHover();

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
        // uiManager.OnAttackButtonHoverExit is already called by HideAttackHover()
        uiManager.SetEnemySelectionPanelActive(true);
        uiManager.UpdateBattleStatus("Select target enemy for " + selectedAttack.attackName + "!");

        List<int> aliveEnemies = GetAliveEnemyIndices();
        uiManager.SetupEnemySelectionButtons(aliveEnemies, enemySpawner.GetEnemyNames(), OnEnemySelected);
    }

    private void OnEnemySelected(int enemyIndex)
    {
        ClearAllEnemyHighlights();

        selectedEnemyIndex = enemyIndex;
        uiManager.SetEnemySelectionPanelActive(false);
        uiManager.SetAttackPanelActive(true);
        StartCoroutine(PerformAttackCoroutine(selectedAttack, selectedEnemyIndex));
    }

    // In StoryBattleManager.cs

    private IEnumerator PerformAttackCoroutine(AttackData attack, int targetEnemyIndex)
    {
        SetAttackButtonsInteractable(false);

        // This now waits for all damage/UI animations within the attack to finish.
        yield return attackHandler.PerformAttack(attack, targetEnemyIndex,
            uiManager.PlayImpactEffect,
            () => uiManager.FlashSprite(Player.GetComponent<SpriteRenderer>(), attack));

        // UpdateHealthDisplays is still useful for a final sync before death checks.
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

        foreach (var routine in deathRoutines)
        {
            yield return routine;
        }

        CheckForBattleEnd();
    }

    private IEnumerator IndividualEnemyDeath(GameObject enemy, int enemyIndex)
    {
        if (enemy == null) yield break;
        Animator animator = enemy.GetComponent<Animator>();
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (animator == null || spriteRenderer == null) yield break;

        animator.SetTrigger("Death");

        yield return new WaitForSeconds(0.1f);
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);

        spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        if (uiManager != null)
        {
            uiManager.DeactivateEnemyUIElement(enemyIndex);
        }
    }


    private bool isPlayerTurn = true;

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
                // Decrement cooldowns at the start of this enemy's action.
                enemyAI.DecrementCooldowns();
                uiManager.UpdateBattleStatus($"{currentEnemy.name.Replace("(Clone)", "")} is thinking...");

                // Create a result object to hold the AI's eventual choice.
                var choiceResult = new AttackChoiceResult();

                // Run the GetSmartAttack coroutine and wait for it to populate the result object.
                yield return StartCoroutine(enemyAI.GetSmartAttack(this, groqHandler, choiceResult));

                AttackData chosenAttack = choiceResult.ChosenAttack;

                if (chosenAttack != null)
                {
                    // Perform the attack chosen by the AI (or its fallback).
                    yield return attackHandler.PerformEnemyAttack(chosenAttack, currentEnemy,
                        uiManager.PlayImpactEffect,
                        () => uiManager.FlashSprite(Player.GetComponent<SpriteRenderer>(), chosenAttack));

                    // Set the cooldown for the attack that was just used.
                    enemyAI.SetCooldownOnAttack(chosenAttack.attackName);
                }
                else
                {
                    // This case happens if the AI fails AND the fallback finds no available moves.
                    uiManager.UpdateBattleStatus($"{currentEnemy.name.Replace("(Clone)", "")} has no available attacks!");
                    yield return new WaitForSeconds(1.0f);
                }

                UpdateHealthDisplays();

                if (currentPlayerHealth <= 0) break;

                yield return new WaitForSeconds(0.5f); // Pause between individual enemy attacks.
            }
        }

        CheckForBattleEnd();
    }

    private void CheckForBattleEnd()
    {
        if (currentPlayerHealth <= 0)
        {
            StartCoroutine(HandlePlayerDefeat());
            return; // Player is defeated, end the battle
        }

        if (GetAliveEnemyIndices().Count == 0)
        {
            // All enemies are defeated
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (currentEnemyHealths[i] <= 0 && Enemies[i] != null)
                {
                    Enemies[i].SetActive(false);
                }
            }
            StartCoroutine(HandleAllEnemiesDefeated());
            return; // Wave is won
        }

        // --- Turn Switching Logic ---
        if (isPlayerTurn)
        {
            // Player's turn has just ended, switch to enemies' turn
            isPlayerTurn = false;
            StartCoroutine(EnemiesTurnCoroutine());
        }
        else
        {
            // Enemies' turn has just ended, switch to player's turn
            isPlayerTurn = true;
            ApplyTurnStartEffects();
            uiManager.UpdateBattleStatus("Choose your next attack!");
            SetAttackButtonsInteractable(true);
            ShowAttackOptions();
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
        yield return new WaitForSeconds(1f); // A brief pause after the last enemy's animation is done.

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
    public void OnEnemyButtonHoverEnter(int enemyIndex)
    {
        if (Enemies.Count > enemyIndex && Enemies[enemyIndex] != null)
        {
            var renderer = Enemies[enemyIndex].GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(1f, 1f, 0.7f);
            }
        }
    }
    public void OnEnemyButtonHoverExit(int enemyIndex)
    {
        if (Enemies.Count > enemyIndex && Enemies[enemyIndex] != null)
        {
            var renderer = Enemies[enemyIndex].GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Revert the sprite's color to white, which means "no tint".
                renderer.color = Color.white;
            }
        }
    }

    private void ClearAllEnemyHighlights()
    {
        foreach (GameObject enemy in Enemies)
        {
            if (enemy != null)
            {
                var renderer = enemy.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = Color.white;
                }
            }
        }
    }
    private void ResetEnemiesForNewWave()
    {
        processedAsDefeated.Clear();
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

    // --- BUG FIX: PERSISTENT HOVER ---
    // Public method to allow other scripts like UpgradeManager to hide the hover UI.
    public void HideAttackHover()
    {
        uiManager.OnAttackButtonHoverExit(ref hoverCoroutine, ref currentHoverInstance);
    }
}