using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryBattleManager : MonoBehaviour
{
    [Header("Battle Setup")]
    [SerializeField] public GameObject Player;
    [SerializeField] public List<GameObject> Enemies = new List<GameObject>();
    [SerializeField] private Transform playerPosition;
    [SerializeField] private string[] enemyNames = new string[4];

    [Header("UI Panels")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject enemySelectionPanel;
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private GameObject enemyUIPanel; // Empty panel to hold enemy UI elements

    [Header("UI Elements")]
    [SerializeField] private Text playerHealthText;
    [SerializeField] private Text battleStatusText;
    [SerializeField] private Text resultText;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private List<GameObject> enemySelectionButtons = new List<GameObject>();

    [Header("Prefabs")]
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private GameObject attackHoverPrefab;
    [SerializeField] private GameObject enemyUIElementPrefab; // GameObject prefab with 2 text components

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource backgroundMusicSource;

    [Header("Battle Settings")]
    [SerializeField] private float playerMaxHealth = 200f;
    [SerializeField] private float enemyMaxHealth = 30f;
    [SerializeField] private float hoverDelay = 0.5f;
    [SerializeField] private Vector3 enemyUIOffset = new Vector3(0, 1.5f, 0); // Offset for UI above enemies

    [Header("Character Database")]
    public CharacterDatabase characterDB;

    private List<AttackData> playerAttacks = new List<AttackData>();
    private Character selectedCharacter;
    private AttackData selectedAttack;
    private int selectedEnemyIndex = -1;
    private float currentPlayerHealth;
    private List<float> currentEnemyHealths = new List<float>();
    private List<GameObject> enemyUIInstances = new List<GameObject>(); // Store UI instances for each enemy
    private GameObject currentHoverInstance;
    private Coroutine hoverCoroutine;

    [Header("Battle Statistics")]
    private float playerDamageDealt = 0f;
    private float playerDamageTaken = 0f;
    private float playerHealingDone = 0f;

    [Header("Wave System")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private float waveProgressionMultiplier = 1.1f;
    [SerializeField] private GameObject[] enemyPrefabs; // Array of different enemy types
    [SerializeField] private Transform[] enemySpawnPoints; // Where enemies spawn

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDistance = 1.25f;

    private Vector3 originalPlayerPosition;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        InitializeStoryBattle();
    }

    private void InitializeStoryBattle()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt("selectedStoryCharacter", 0);

        if (characterDB != null)
        {
            selectedCharacter = characterDB.GetCharacter(selectedCharacterIndex);
            SetCharacter(Player, selectedCharacter);
            playerAttacks = AttackDataManager.Instance.GetAttacksForCharacter(selectedCharacter.characterName);
        }
        else
        {
            Debug.LogError("Could not find CharacterDatabase!");
            return;
        }

        currentPlayerHealth = playerMaxHealth;
        currentWave = 1; // Initialize wave counter

        // Keep your existing enemy setup for now
        SpawnWaveEnemies();
        InitializeEnemyHealth();
        CreateEnemyUI();
        SetupPlayer();

        if (enemySelectionPanel != null)
            enemySelectionPanel.SetActive(false);

        UpdateHealthDisplays();
        battleStatusText.text = $"Wave {currentWave} - Choose your attack!";
        ShowAttackOptions();
    }

    private void InitializeEnemyHealth()
    {
        currentEnemyHealths.Clear();

        // Scale enemy health based on wave
        float scaledHealth = enemyMaxHealth * Mathf.Pow(waveProgressionMultiplier, currentWave - 1);

        for (int i = 0; i < Enemies.Count; i++)
        {
            currentEnemyHealths.Add(scaledHealth);
        }
    }

    private void SpawnWaveEnemies()
    {
        // Clear existing enemies
        foreach (GameObject enemy in Enemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        Enemies.Clear();

        // Scale enemy count based on wave (early waves: 1-2, later waves: 2-4)
        int enemiesToSpawn;
        if (currentWave <= 3)
        {
            enemiesToSpawn = Random.Range(1, 3); // Waves 1-3: 1-2 enemies
        }
        else
        {
            enemiesToSpawn = Random.Range(2, 5); // Waves 4+: 2-4 enemies
        }
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, enemySpawnPoints.Length);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (i < enemySpawnPoints.Length)
            {
                // Pick random enemy prefab (cycles through available prefabs)
                int randomPrefabIndex = Random.Range(0, enemyPrefabs.Length);
                GameObject newEnemy = Instantiate(enemyPrefabs[randomPrefabIndex],
                                                enemySpawnPoints[i].position,
                                                Quaternion.identity);
                Enemies.Add(newEnemy);
            }
        }

        // Update enemy names using prefab names
        for (int i = 0; i < Enemies.Count; i++)
        {
            enemyNames[i] = Enemies[i].name.Replace("(Clone)", "").Trim();
        }
    }

    private void CreateEnemyUI()
    {
        if (enemyUIPanel == null) return;

        enemyUIInstances.Clear();

        for (int i = 0; i < Enemies.Count; i++)
        {
            if (enemyUIElementPrefab != null && Enemies[i] != null)
            {
                // Create UI element as child of the EnemyUI panel
                GameObject uiInstance = Instantiate(enemyUIElementPrefab, enemyUIPanel.transform);

                // Position the element above the enemy using screen coordinates
                RectTransform uiRect = uiInstance.GetComponent<RectTransform>();
                if (uiRect != null)
                {
                    Vector3 enemyWorldPos = Enemies[i].transform.position + enemyUIOffset;
                    uiInstance.transform.position = enemyWorldPos;
                }

                // Set enemy name and health
                Text[] texts = uiInstance.GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.Contains("Name"))
                    {
                        text.text = i < enemyNames.Length ? enemyNames[i] : $"Enemy {i + 1}";
                    }
                    else if (text.name.Contains("Health"))
                    {
                        text.text = $"HP: {currentEnemyHealths[i]}";
                    }
                }

                enemyUIInstances.Add(uiInstance);
            }
        }
    }

    private void SetupPlayer()
    {
        if (playerPosition != null)
        {
            Player.transform.position = playerPosition.position;
            originalPlayerPosition = Player.transform.position; // Store original position
        }
        FlipSprite(Player, true);
    }

    private void SetCharacter(GameObject playerObject, Character character)
    {
        if (character == null) return;

        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = character.characterSprite;

        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            string overridePath = "Animations/" + character.characterName + "Override";
            AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);
            if (overrideController != null)
                animator.runtimeAnimatorController = overrideController;
            else
                Debug.LogWarning("No Animator Override Controller found for " + character.characterName);
        }
    }

    private void FlipSprite(GameObject player, bool faceRight)
    {
        SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = faceRight;
    }

    private void UpdateHealthDisplays()
    {
        playerHealthText.text = "Player HP: " + Mathf.Round(currentPlayerHealth);

        // Update enemy UI panels and reposition them
        for (int i = 0; i < enemyUIInstances.Count && i < currentEnemyHealths.Count; i++)
        {
            if (enemyUIInstances[i] != null)
            {
                // Update health text with rounded values
                Text[] texts = enemyUIInstances[i].GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.Contains("Health"))
                    {
                        text.text = $"HP: {Mathf.Round(currentEnemyHealths[i])}";
                    }
                }

                // Hide UI if enemy is dead
                enemyUIInstances[i].SetActive(currentEnemyHealths[i] > 0);
            }
        }
    }

    private void OnAttackSelected(AttackData attack)
    {
        selectedAttack = attack;

        if (attack.attackType == AttackType.Heal)
        {
            PerformAttack(attack, -1);
            return;
        }

        // Area attacks don't need target selection - hit all enemies
        if (attack.attackType == AttackType.AreaEffect)
        {
            PerformAttack(attack, -1); // Use -1 to indicate area attack
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
            PerformAttack(attack, aliveEnemies[0]);
            return;
        }

        ShowEnemySelection();
    }

    private List<int> GetAliveEnemyIndices()
    {
        List<int> aliveIndices = new List<int>();
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            if (currentEnemyHealths[i] > 0)
            {
                aliveIndices.Add(i);
            }
        }
        return aliveIndices;
    }

    private void ShowEnemySelection()
    {
        if (enemySelectionPanel == null) return;

        attackPanel.SetActive(false);
        OnAttackButtonHoverExit(); // Hide any active hover UI
        enemySelectionPanel.SetActive(true);
        battleStatusText.text = "Select target enemy for " + selectedAttack.attackName + "!";

        List<int> aliveEnemies = GetAliveEnemyIndices();
        SetupEnemySelectionButtons(aliveEnemies);
    }

    private void SetupEnemySelectionButtons(List<int> aliveEnemies)
    {
        for (int i = 0; i < enemySelectionButtons.Count; i++)
        {
            if (i < aliveEnemies.Count && enemySelectionButtons[i] != null)
            {
                int enemyIndex = aliveEnemies[i];
                enemySelectionButtons[i].gameObject.SetActive(true);

                Text buttonText = enemySelectionButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = enemyNames[enemyIndex];
                }

                Button button = enemySelectionButtons[i].GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    int capturedIndex = enemyIndex;
                    button.onClick.AddListener(() => OnEnemySelected(capturedIndex));
                }
            }
            else if (enemySelectionButtons[i] != null)
            {
                enemySelectionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnEnemySelected(int enemyIndex)
    {
        selectedEnemyIndex = enemyIndex;
        enemySelectionPanel.SetActive(false);
        attackPanel.SetActive(true);
        PerformAttack(selectedAttack, selectedEnemyIndex);
    }

    private void PerformAttack(AttackData attack, int targetEnemyIndex)
    {
        SetAttackButtonsInteractable(false);

        if (Random.Range(0f, 1f) > attack.accuracy)
        {
            battleStatusText.text = attack.attackName + " missed!";
            StartCoroutine(HandleMissedAttack(attack));
            return;
        }

        if (attack.attackType == AttackType.Heal)
        {
            HandleHealAttack(attack);
            StartCoroutine(ShowAttackAnimation(Player.GetComponent<Animator>(), attack, targetEnemyIndex));
        }
        else if (attack.attackType == AttackType.AreaEffect)
        {
            // Multi-target attack - hit all alive enemies
            StartCoroutine(HandleAreaAttack(attack));
        }
        else if (attack.attackType == AttackType.MoveAndHit)
        {
            // Move to enemy, then attack
            StartCoroutine(MoveToEnemyAndAttack(attack, targetEnemyIndex));
        }
        else
        {
            // Regular ranged attack - no movement needed
            HandleDamageAttack(attack, targetEnemyIndex);
            StartCoroutine(ShowAttackAnimation(Player.GetComponent<Animator>(), attack, targetEnemyIndex));
        }

        // Don't update health displays here for area attacks - HandleAreaAttack will do it
        if (attack.attackType != AttackType.AreaEffect)
        {
            UpdateHealthDisplays();
        }
    }

    private IEnumerator HandleAreaAttack(AttackData attack)
    {
        List<int> aliveEnemies = GetAliveEnemyIndices();
        if (aliveEnemies.Count == 0)
        {
            CheckForBattleEnd();
            yield break;
        }

        battleStatusText.text = "You used " + attack.attackName + " on all enemies!";

        // Play attack animation first
        yield return StartCoroutine(ShowAttackAnimation(Player.GetComponent<Animator>(), attack, -1));

        // Apply damage to each alive enemy individually
        float totalDamageDealt = 0f;
        foreach (int enemyIndex in aliveEnemies)
        {
            // Calculate damage for this specific enemy (including crit chance per enemy)
            float finalDamage = attack.damage;
            bool isCrit = Random.Range(0f, 1f) < attack.critChance;

            if (isCrit)
            {
                finalDamage *= 2f;
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayCritSound();
            }

            // Apply damage to this enemy
            currentEnemyHealths[enemyIndex] = Mathf.Max(0, currentEnemyHealths[enemyIndex] - finalDamage);
            totalDamageDealt += finalDamage;

            // Show individual hit effect for each enemy
            if (enemyIndex < Enemies.Count && Enemies[enemyIndex] != null)
            {
                StartCoroutine(ShowDirectEffect(Enemies[enemyIndex], attack));
            }
        }

        // Handle self-damage if any (only once, not per enemy)
        if (attack.doubleEdgeDamage > 0)
        {
            currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - attack.doubleEdgeDamage);
            battleStatusText.text += "\nBut you hurt yourself for " + attack.doubleEdgeDamage + " damage!";
            playerDamageTaken += attack.doubleEdgeDamage;
        }

        playerDamageDealt += totalDamageDealt;

        // Update health displays after all damage is applied
        UpdateHealthDisplays();

        // Small delay before checking battle end
        yield return new WaitForSeconds(0.5f);
        CheckForBattleEnd();
    }

    private IEnumerator MoveToEnemyAndAttack(AttackData attack, int targetEnemyIndex)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= Enemies.Count || Enemies[targetEnemyIndex] == null)
        {
            battleStatusText.text = "Invalid target!";
            SetAttackButtonsInteractable(true);
            yield break;
        }

        // Calculate target position near the enemy
        Vector3 enemyPos = Enemies[targetEnemyIndex].transform.position;
        Vector3 targetPos = enemyPos + Vector3.left * attackDistance; // Stand to the left of enemy

        // Move to enemy
        yield return StartCoroutine(MovePlayerTo(targetPos));

        // Perform the attack
        HandleDamageAttack(attack, targetEnemyIndex);

        // Show attack animation
        yield return StartCoroutine(ShowAttackAnimation(Player.GetComponent<Animator>(), attack, targetEnemyIndex));

        // Move back to original position
        yield return StartCoroutine(MovePlayerTo(originalPlayerPosition));
    }

    // Add this new coroutine for smooth movement:
    private IEnumerator MovePlayerTo(Vector3 targetPosition)
    {
        Vector3 startPosition = Player.transform.position;
        float journey = 0f;

        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;
            Player.transform.position = Vector3.Lerp(startPosition, targetPosition, journey);
            yield return null;
        }

        Player.transform.position = targetPosition;
    }

    private void HandleHealAttack(AttackData attack)
    {
        float healAmount = Mathf.Min(attack.damage, playerMaxHealth - currentPlayerHealth);
        currentPlayerHealth += healAmount;
        battleStatusText.text = "You used " + attack.attackName + " and recovered " + Mathf.Round(healAmount) + " HP!";

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX(attack.soundEffectName);

        playerHealingDone += healAmount;
    }

    private void HandleDamageAttack(AttackData attack, int targetEnemyIndex)
    {
        if (targetEnemyIndex < 0 || targetEnemyIndex >= currentEnemyHealths.Count || currentEnemyHealths[targetEnemyIndex] <= 0)
        {
            battleStatusText.text = "Invalid target!";
            SetAttackButtonsInteractable(true);
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX(attack.soundEffectName);

        float finalDamage = attack.damage;
        bool isCrit = Random.Range(0f, 1f) < attack.critChance;

        if (isCrit)
        {
            finalDamage *= 2f;
            if (GameManager.Instance != null)
                GameManager.Instance.PlayCritSound();
        }

        currentEnemyHealths[targetEnemyIndex] = Mathf.Max(0, currentEnemyHealths[targetEnemyIndex] - finalDamage);

        string critText = isCrit ? " It's a critical hit!" : "";
        battleStatusText.text = "You used " + attack.attackName + " on " + enemyNames[targetEnemyIndex] + "!" + critText;

        if (attack.doubleEdgeDamage > 0)
        {
            currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - attack.doubleEdgeDamage);
            battleStatusText.text += "\nBut you hurt yourself for " + attack.doubleEdgeDamage + " damage!";
            playerDamageTaken += attack.doubleEdgeDamage;
        }

        playerDamageDealt += finalDamage;
    }

    private IEnumerator HandleMissedAttack(AttackData attack)
    {
        yield return new WaitForSeconds(1.0f);

        if (attack.doubleEdgeDamage > 0)
        {
            currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - attack.doubleEdgeDamage);

            Animator playerAnimator = Player.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Hit");
            }

            SpriteRenderer playerRenderer = Player.GetComponent<SpriteRenderer>();
            if (playerRenderer != null)
            {
                StartCoroutine(FlashSprite(playerRenderer, attack));
            }
            playerDamageTaken += attack.doubleEdgeDamage;

            yield return new WaitForSeconds(0.5f);
        }
        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("Audio/SFX/General/Miss");
        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("General/HitSound");

        UpdateHealthDisplays();
        CheckForBattleEnd();
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack, int targetEnemyIndex)
    {
        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        yield return new WaitForSeconds(attack.effectDelay);

        if (attack.attackType == AttackType.Heal)
        {
            yield return StartCoroutine(ShowDirectEffect(Player, attack));
        }
        else if (targetEnemyIndex >= 0 && targetEnemyIndex < Enemies.Count)
        {
            yield return StartCoroutine(ShowDirectEffect(Enemies[targetEnemyIndex], attack));
        }

        yield return new WaitForSeconds(0.5f);
        CheckForBattleEnd();
    }

    private IEnumerator ShowDirectEffect(GameObject target, AttackData attack)
    {
        GameObject effectPrefab = AttackDataManager.Instance.GetEffectPrefabForAttack(attack);
        if (effectPrefab == null) yield break;

        GameObject attackEffect = Instantiate(effectPrefab, attack.effectOffset, Quaternion.identity);
        float effectDuration = 0.5f;

        Animator effectAnimator = attackEffect.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            effectDuration = effectAnimator.GetCurrentAnimatorStateInfo(0).length;
        }

        PlayImpactEffect(target, attack);
        yield return new WaitForSeconds(effectDuration);
        Destroy(attackEffect);
    }

    private void PlayImpactEffect(GameObject target, AttackData attack)
    {
        Animator targetAnimator = target.GetComponent<Animator>();
        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();

        if (attack.attackType == AttackType.Heal)
        {
            StartCoroutine(FlashSprite(targetRenderer, attack));
            return;
        }

        if (targetAnimator != null)
            targetAnimator.SetTrigger("Hit");

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("General/HitSound");

        if (targetRenderer != null)
            StartCoroutine(FlashSprite(targetRenderer, attack));
    }

    private IEnumerator FlashSprite(SpriteRenderer renderer, AttackData attack)
    {
        Color originalColor = renderer.color;
        for (int i = 0; i < 3; i++)
        {
            renderer.color = attack.flashColor;
            yield return new WaitForSeconds(attack.flashInterval);
            renderer.color = originalColor;
            yield return new WaitForSeconds(attack.flashInterval);
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
            // Hide all dead enemies before showing victory
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
            // Hide any enemies that just died
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (currentEnemyHealths[i] <= 0 && Enemies[i] != null && Enemies[i].activeInHierarchy)
                {
                    StartCoroutine(HideEnemyAfterDeath(i));
                }
            }

            SetAttackButtonsInteractable(true);
            battleStatusText.text = "Choose your next attack!";
        }
    }

    private IEnumerator HideEnemyAfterDeath(int enemyIndex)
    {
        yield return new WaitForSeconds(1f); // Wait for death animation
        if (Enemies[enemyIndex] != null)
        {
            Enemies[enemyIndex].SetActive(false);
        }
    }


    private IEnumerator HandlePlayerDefeat()
    {
        battleStatusText.text = "You have been defeated!";

        Animator playerAnimator = Player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Death");
            yield return new WaitForSeconds(2f);
        }

        ShowEndScreen(false);
    }

    private IEnumerator HandleAllEnemiesDefeated()
    {
        battleStatusText.text = $"Wave {currentWave} cleared!";

        // Death animations
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

        // Hide dead enemies
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (currentEnemyHealths[i] <= 0 && Enemies[i] != null)
            {
                Enemies[i].SetActive(false);
            }
        }

        // Check if more waves remain
        if (currentWave < maxWaves)
        {
            StartNextWave();
        }
        else
        {
            // All waves completed - victory!
            ShowEndScreen(true);
        }
    }

    private void StartNextWave()
    {
        currentWave++;
        battleStatusText.text = $"Preparing Wave {currentWave}...";

        StartCoroutine(WaveTransition());
    }

    private IEnumerator WaveTransition()
    {
        yield return new WaitForSeconds(0.5f);

        // Reactivate and reset existing enemies
        ResetEnemiesForNewWave();

        // Restore some player health between waves
        float healAmount = playerMaxHealth * 0.1f;
        currentPlayerHealth = Mathf.Min(playerMaxHealth, currentPlayerHealth + healAmount);

        UpdateHealthDisplays();
        battleStatusText.text = $"Wave {currentWave} - Choose your attack!";

        SetAttackButtonsInteractable(true);
    }

    private void ResetEnemiesForNewWave()
    {
        // Reactivate all enemies
        foreach (GameObject enemy in Enemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }

        // Reset enemy health with wave scaling
        float scaledHealth = enemyMaxHealth * Mathf.Pow(waveProgressionMultiplier, currentWave - 1);
        for (int i = 0; i < currentEnemyHealths.Count; i++)
        {
            currentEnemyHealths[i] = scaledHealth;
        }

        // Update enemy names using prefab names (not generic wave names)
        for (int i = 0; i < Enemies.Count && i < enemyNames.Length; i++)
        {
            enemyNames[i] = Enemies[i].name.Replace("(Clone)", "").Trim();
        }

        // Recreate UI with new names and health
        CreateEnemyUI();
    }

    private void ShowEndScreen(bool playerWon)
    {
        if (endScreenPanel != null)
        {
            battlePanel.SetActive(false);
            endScreenPanel.SetActive(true);

            if (playerWon)
            {
                resultText.text = $"VICTORY!\nCompleted all {maxWaves} waves!";
            }
            else
            {
                resultText.text = $"DEFEAT!\nReached Wave {currentWave}";
            }

            // Show stats including wave reached
            foreach (Text text in endScreenPanel.GetComponentsInChildren<Text>())
            {
                if (text.name.Contains("WaveReached"))
                    text.text = "Wave Reached: " + currentWave;
                // ... rest of your existing stat code
            }

            SetupEndScreenButtons(playerWon);
        }
    }

    private void SetupEndScreenButtons(bool playerWon)
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("StartScreen"));
        }

        if (continueButton != null)
        {
            if (playerWon)
            {
                continueButton.onClick.AddListener(() => SceneManager.LoadScene("StoryMode"));
            }
            else
            {
                continueButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            }
        }
    }

    private void SetAttackButtonsInteractable(bool interactable)
    {
        foreach (Transform child in attackPanel.transform)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private void ShowAttackOptions()
    {
        foreach (Transform child in attackPanel.transform)
        {
            if (child.gameObject != attackHoverPrefab)
                Destroy(child.gameObject);
        }

        CreateAttackButtons();
    }

    private void CreateAttackButtons()
    {
        float startY = -75;
        float xPosition = 692;
        float yStep = -100;

        for (int i = 0; i < playerAttacks.Count; i++)
        {
            AttackData attack = playerAttacks[i];
            GameObject buttonObj = Instantiate(attackButtonPrefab, attackPanel.transform);

            SetupAttackButton(buttonObj, attack);
            PositionAttackButton(buttonObj, xPosition, startY + (yStep * i));
            AddHoverToButton(buttonObj, attack, buttonObj.GetComponent<RectTransform>());

            buttonObj.SetActive(true);
        }
    }

    private void SetupAttackButton(GameObject buttonObj, AttackData attack)
    {
        Button button = buttonObj.GetComponent<Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        if (buttonText != null)
        {
            buttonText.text = attack.attackName;
        }

        button.onClick.AddListener(() => OnAttackSelected(attack));
    }

    private void PositionAttackButton(GameObject buttonObj, float x, float y)
    {
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(x, y);
    }

    private void AddHoverToButton(GameObject buttonObj, AttackData attack, RectTransform buttonRect)
    {
        UnityEngine.EventSystems.EventTrigger eventTrigger = buttonObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnAttackButtonHoverEnter(attack, buttonRect));
        eventTrigger.triggers.Add(pointerEnter);

        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => OnAttackButtonHoverExit());
        eventTrigger.triggers.Add(pointerExit);
    }

    private void OnAttackButtonHoverEnter(AttackData attack, RectTransform buttonRect)
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(ShowHoverAfterDelay(attack, buttonRect));
    }

    private void OnAttackButtonHoverExit()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        if (currentHoverInstance != null)
        {
            Destroy(currentHoverInstance);
            currentHoverInstance = null;
        }
    }

    private IEnumerator ShowHoverAfterDelay(AttackData attack, RectTransform buttonRect)
    {
        yield return new WaitForSeconds(hoverDelay);

        currentHoverInstance = Instantiate(attackHoverPrefab, attackPanel.transform);
        SetupHoverText(attack);
        PositionHoverUI(buttonRect);
        DisableHoverRaycast();

        hoverCoroutine = null;
    }

    private void SetupHoverText(AttackData attack)
    {
        Text[] hoverTexts = currentHoverInstance.GetComponentsInChildren<Text>();
        foreach (Text text in hoverTexts)
        {
            if (text.name.Contains("Damage") || text.name.Contains("damage"))
            {
                text.text = attack.damage.ToString() + " dmg";
            }
            else if (text.name.Contains("Description") || text.name.Contains("description"))
            {
                text.text = BuildAttackDescription(attack);
            }
        }
    }

    private string BuildAttackDescription(AttackData attack)
    {
        string description = attack.description;
        description += "\nCrit: " + (attack.critChance * 100f).ToString("F0") + "%";
        description += "\nAccuracy: " + (attack.accuracy * 100f).ToString("F0") + "%";

        if (attack.doubleEdgeDamage > 0)
        {
            description += "\nSelf-damage: " + attack.doubleEdgeDamage;
        }

        if (attack.canSelfKO)
        {
            description += "\nSelf-KO Chance: " + (attack.selfKOFailChance * 100f).ToString("F0") + "%";
        }

        return description;
    }

    private void PositionHoverUI(RectTransform buttonRect)
    {
        RectTransform hoverRect = currentHoverInstance.GetComponent<RectTransform>();
        Vector2 buttonPos = buttonRect.anchoredPosition;
        hoverRect.anchoredPosition = new Vector2(buttonPos.x, buttonPos.y + 125f);
    }

    private void DisableHoverRaycast()
    {
        Graphic[] graphics = currentHoverInstance.GetComponentsInChildren<Graphic>();
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }
}