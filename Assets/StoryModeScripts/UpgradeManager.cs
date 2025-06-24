using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject upgradeSelectionPanel;
    [SerializeField] private GameObject shadowPanel;
    [SerializeField] private GameObject upgradeTemplate;
    [SerializeField] private Text waveCompleteText;
    [SerializeField] private Button skipUpgradeButton;

    // New UI References for Run Info Panel
    [Header("Run Info UI References")]
    [SerializeField] private GameObject runInfoPanel; // The main RunInfoPanel GameObject
    [SerializeField] private Text runInfoQuestionText; // "RUN INFO" text
    [SerializeField] private Transform boonsContainer; // The container for upgrade entries (UISlider content)
    [SerializeField] private Transform abilitiesContainer; // The container for ability entries (UISlider content)
    [SerializeField] private GameObject runInfoUpgradeTextTemplate; // A simple Text GameObject prefab for each upgrade entry

    [Header("Upgrade Database")]
    // These lists are for upgrades you manually assign in the inspector (e.g., stat boosts)
    [SerializeField] private List<UpgradeData> staticCommonUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> staticRareUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> staticEpicUpades = new List<UpgradeData>(); // Corrected typo
    [SerializeField] private List<UpgradeData> staticLegendaryUpgrades = new List<UpgradeData>();

    // These lists will be populated dynamically by the code for attack-unlocking upgrades
    private List<UpgradeData> dynamicCommonUpgrades = new List<UpgradeData>();
    private List<UpgradeData> dynamicRareUpgrades = new List<UpgradeData>();
    private List<UpgradeData> dynamicEpicUpgrades = new List<UpgradeData>();
    private List<UpgradeData> dynamicLegendaryUpgrades = new List<UpgradeData>();

    // New: Separate list for all dynamically generated attack upgrades, regardless of rarity
    private List<UpgradeData> allDynamicAttackUpgrades = new List<UpgradeData>();

    // This combined pool is what the game uses to select upgrades from
    private List<UpgradeData> allAvailableUpgradesPool = new List<UpgradeData>();


    [Header("Rarity Chances (Wave Dependent)")]
    [SerializeField] private AnimationCurve commonChanceCurve;
    [SerializeField] private AnimationCurve rareChanceCurve;
    [SerializeField] private AnimationCurve epicChanceCurve;
    [SerializeField] private AnimationCurve legendaryChanceCurve;

    [Header("Settings")]
    [SerializeField] private int upgradeChoicesCount = 3;
    [SerializeField] private bool allowSkipUpgrade = true;
    [SerializeField] private float animationDelay = 0.2f;

    // Player's current upgrades
    private Dictionary<UpgradeData, int> playerUpgrades = new Dictionary<UpgradeData, int>();
    private List<GameObject> currentUpgradeButtons = new List<GameObject>();

    // Events
    public System.Action<UpgradeData> OnUpgradeSelected;
    public System.Action OnUpgradeSkipped;
    public System.Action OnUpgradeSelectionComplete;

    // Reference to battle manager for applying upgrades
    private StoryBattleManager battleManager;

    private void Awake()
    {
        battleManager = FindObjectOfType<StoryBattleManager>();

        if (upgradeSelectionPanel != null)
            upgradeSelectionPanel.SetActive(false);
        if (shadowPanel != null)
            shadowPanel.SetActive(false);

        if (runInfoPanel != null)
            runInfoPanel.SetActive(false);

        SetupSkipButton();
        SetupDefaultCurves();
        InitializeDynamicUpgrades(); // Generate attack upgrades (and populate allDynamicAttackUpgrades)

        // Populate the combined pool when the manager starts
        PopulateAllAvailableUpgradesPool();
    }

    private void PopulateAllAvailableUpgradesPool()
    {
        // This pool is used for the *general* pool of upgrades that are always considered.
        // Attack upgrades are handled separately in GenerateRandomUpgrades.
        allAvailableUpgradesPool.Clear(); // Clear before re-populating to prevent duplicates on scene reload

        // Add static (manually assigned) upgrades
        allAvailableUpgradesPool.AddRange(staticCommonUpgrades);
        allAvailableUpgradesPool.AddRange(staticRareUpgrades);
        allAvailableUpgradesPool.AddRange(staticEpicUpades);
        allAvailableUpgradesPool.AddRange(staticLegendaryUpgrades);

        // NOTE: Dynamic attack upgrades are *not* added here directly anymore.
        // They will be conditionally added in GenerateRandomUpgrades based on wave number.
    }


    private void SetupSkipButton()
    {
        if (skipUpgradeButton != null)
        {
            skipUpgradeButton.onClick.AddListener(SkipUpgrade);
            skipUpgradeButton.gameObject.SetActive(allowSkipUpgrade);
        }
    }

    private void SetupDefaultCurves()
    {
        if (commonChanceCurve.keys.Length == 0)
        {
            commonChanceCurve = AnimationCurve.Linear(1, 0.7f, 5, 0.3f);
        }
        if (rareChanceCurve.keys.Length == 0)
        {
            rareChanceCurve = AnimationCurve.Linear(1, 0.25f, 5, 0.4f);
        }
        if (epicChanceCurve.keys.Length == 0)
        {
            epicChanceCurve = AnimationCurve.Linear(1, 0.05f, 5, 0.25f);
        }
        if (legendaryChanceCurve.keys.Length == 0)
        {
            legendaryChanceCurve = AnimationCurve.Linear(1, 0.0f, 5, 0.05f);
        }
    }

    private void InitializeDynamicUpgrades()
    {
        dynamicCommonUpgrades.Clear();
        dynamicRareUpgrades.Clear();
        dynamicEpicUpgrades.Clear();
        dynamicLegendaryUpgrades.Clear();
        allDynamicAttackUpgrades.Clear(); // Clear this master list too

        List<string> characterNames = new List<string>
        {
            "Knight", "Archer", "Water", "Fire", "Wind", "Necromancer", "Crystal", "Ground"
        };

        foreach (string charName in characterNames)
        {
            List<AttackData> unlockableAttacks = StoryAttackDataManager.Instance.GetUnlockableAttacksForCharacter(charName);
            foreach (AttackData attack in unlockableAttacks)
            {
                UpgradeData attackUpgrade = ScriptableObject.CreateInstance<UpgradeData>();

                attackUpgrade.upgradeName = $"Unlock {attack.attackName}";
                attackUpgrade.description = $"Grants the '{attack.attackName}' attack: {attack.description}";
                attackUpgrade.upgradeIcon = null;
                attackUpgrade.upgradeType = UpgradeType.SpecialAttack;
                attackUpgrade.rarity = GetRarityForAttack(attack.attackName);
                attackUpgrade.grantsNewAttack = true;
                attackUpgrade.newAttackName = attack.attackName;
                attackUpgrade.canStack = false;
                attackUpgrade.maxStacks = 1;
                attackUpgrade.isUnique = true;

                attackUpgrade.SetRarityColorsInternal();

                // Add to dynamic lists for potential future use or separation if needed,
                // but primarily add to the 'allDynamicAttackUpgrades' for easier access.
                switch (attackUpgrade.rarity)
                {
                    case UpgradeRarity.Common:
                        dynamicCommonUpgrades.Add(attackUpgrade);
                        break;
                    case UpgradeRarity.Rare:
                        dynamicRareUpgrades.Add(attackUpgrade);
                        break;
                    case UpgradeRarity.Epic:
                        dynamicEpicUpgrades.Add(attackUpgrade);
                        break;
                    case UpgradeRarity.Legendary:
                        dynamicLegendaryUpgrades.Add(attackUpgrade);
                        break;
                }
                allDynamicAttackUpgrades.Add(attackUpgrade); // Add to the master list of all attack upgrades
            }
        }
        Debug.Log($"Dynamically initialized {allDynamicAttackUpgrades.Count} attack upgrades.");
    }

    // Helper to assign a rarity to an attack-unlocking upgrade.
    private UpgradeRarity GetRarityForAttack(string attackName)
    {
        switch (attackName)
        {
            case "Sacrificial Blade":
            case "Final Offering":
            case "Inferno Sacrifice":
            case "Tempest Collapse":
            case "Titanic Reckoning":
            case "Prismatic Overload":
            case "Water Ball":
            case "Green Beam":
                return UpgradeRarity.Legendary;
            case "Dragon Slash":
            case "Arrow Shower":
            case "Water Dance":
            case "Fire Combo":
            case "Wind Barrage":
            case "Red Lightning":
            case "Crystal Eruption":
            case "Rock Slide":
                return UpgradeRarity.Epic;
            case "Warrior Slash":
            case "Impale Arrow":
            case "Heal":
            case "Spin Slash":
            case "Tornado":
            case "Blood Spike":
            case "Crystal Hammer":
            case "Punch Combo":
                return UpgradeRarity.Rare;
            default:
                return UpgradeRarity.Common;
        }
    }


    public void ShowUpgradeSelection(int currentWave)
    {
        if (upgradeSelectionPanel == null) return;

        // --- BUG FIX: PERSISTENT HOVER ---
        if (battleManager != null)
        {
            // Hide any attack hover UI that might be stuck
            battleManager.HideAttackHover();
        }

        ClearCurrentUpgrades();
        List<UpgradeData> selectedUpgrades = GenerateRandomUpgrades(currentWave);

        upgradeSelectionPanel.SetActive(true);
        if (shadowPanel != null)
            shadowPanel.SetActive(true);

        if (waveCompleteText != null)
        {
            waveCompleteText.text = $"Wave {currentWave} Complete!\nChoose an Upgrade:";
        }

        StartCoroutine(CreateUpgradeButtons(selectedUpgrades));
    }

    private void ClearCurrentUpgrades()
    {
        foreach (GameObject button in currentUpgradeButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentUpgradeButtons.Clear();
    }

    private List<UpgradeData> GenerateRandomUpgrades(int currentWave)
    {
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();

        // Start with the pool of regular (static) upgrades
        List<UpgradeData> availableUpgradesPool = GetAvailableUpgradesFromPool(allAvailableUpgradesPool);

        // Check if it's an "ability wave" (every 2 waves)
        bool isAbilityWave = (currentWave % 2 == 0);

        // If it's an ability wave, temporarily add available attack upgrades to the pool
        if (isAbilityWave)
        {
            List<UpgradeData> availableAttackUpgrades = GetAvailableUpgradesFromPool(allDynamicAttackUpgrades);
            availableUpgradesPool.AddRange(availableAttackUpgrades);
        }

        // Create a temporary working list to remove selected upgrades from *this current selection round*
        List<UpgradeData> tempWorkingList = new List<UpgradeData>(availableUpgradesPool);

        for (int i = 0; i < upgradeChoicesCount; i++)
        {
            if (tempWorkingList.Count == 0) break; // No more unique upgrades to choose from

            UpgradeRarity desiredRarity = GetRandomRarity(currentWave, tempWorkingList); // Pass tempWorkingList for rarity check
            UpgradeData selectedUpgrade = GetRandomUpgradeOfRarity(desiredRarity, tempWorkingList);

            if (selectedUpgrade != null)
            {
                selectedUpgrades.Add(selectedUpgrade);
                tempWorkingList.Remove(selectedUpgrade); // Ensure it's not picked again in this selection
            }
            else
            {
                // If a specific rarity could not be found, try to pick any available upgrade from the remaining
                if (tempWorkingList.Count > 0)
                {
                    UpgradeData fallback = tempWorkingList[Random.Range(0, tempWorkingList.Count)];
                    selectedUpgrades.Add(fallback);
                    tempWorkingList.Remove(fallback);
                }
                else
                {
                    break; // No more available upgrades
                }
            }
        }

        // Final fallback: Ensure 'upgradeChoicesCount' items if still possible and unique options exist
        while (selectedUpgrades.Count < upgradeChoicesCount && availableUpgradesPool.Except(selectedUpgrades).Any())
        {
            List<UpgradeData> potentialFillers = availableUpgradesPool.Except(selectedUpgrades).ToList();
            if (potentialFillers.Count > 0)
            {
                selectedUpgrades.Add(potentialFillers[Random.Range(0, potentialFillers.Count)]);
            }
            else
            {
                break; // No more unique upgrades to add
            }
        }


        return selectedUpgrades;
    }

    private List<UpgradeData> GetAvailableUpgradesFromPool(List<UpgradeData> pool)
    {
        return pool.Where(upgrade =>
            !(upgrade.isUnique && playerUpgrades.ContainsKey(upgrade)) &&
            !(playerUpgrades.ContainsKey(upgrade) && playerUpgrades[upgrade] >= upgrade.maxStacks)
        ).ToList();
    }


    private UpgradeRarity GetRandomRarity(int currentWave, List<UpgradeData> currentPoolForSelection)
    {
        float commonChance = commonChanceCurve.Evaluate(currentWave);
        float rareChance = rareChanceCurve.Evaluate(currentWave);
        float epicChance = epicChanceCurve.Evaluate(currentWave);
        float legendaryChance = legendaryChanceCurve.Evaluate(currentWave);

        float totalChance = commonChance + rareChance + epicChance + legendaryChance;
        float randomValue = Random.Range(0f, totalChance);

        // Try to return a rarity that actually has available upgrades in the current pool
        if (randomValue < legendaryChance && currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Legendary))
            return UpgradeRarity.Legendary;
        if (randomValue < legendaryChance + epicChance && currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Epic))
            return UpgradeRarity.Epic;
        if (randomValue < legendaryChance + epicChance + rareChance && currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Rare))
            return UpgradeRarity.Rare;
        if (currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Common))
            return UpgradeRarity.Common;

        // Fallback: if no upgrades of the rolled rarity are available in the current pool,
        if (currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Epic)) return UpgradeRarity.Epic;
        if (currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Rare)) return UpgradeRarity.Rare;
        if (currentPoolForSelection.Any(u => u.rarity == UpgradeRarity.Common)) return UpgradeRarity.Common;

        // Last resort: If no upgrades are available AT ALL in the current pool,
        return UpgradeRarity.Common;
    }

    private UpgradeData GetRandomUpgradeOfRarity(UpgradeRarity rarity, List<UpgradeData> poolToDrawFrom)
    {
        // Filter from the provided pool (which is already pre-filtered for uniqueness/max stacks)
        List<UpgradeData> raritySpecificUpgrades = poolToDrawFrom.Where(u => u.rarity == rarity).ToList();

        if (raritySpecificUpgrades.Count > 0)
        {
            return raritySpecificUpgrades[Random.Range(0, raritySpecificUpgrades.Count)];
        }
        else
        {
            // Fallback logic: if no upgrades of the desired rarity are in the current pool,
            // try lower rarities from the *same pool*.
            if (rarity == UpgradeRarity.Legendary && poolToDrawFrom.Any(u => u.rarity == UpgradeRarity.Epic))
            {
                return GetRandomUpgradeOfRarity(UpgradeRarity.Epic, poolToDrawFrom);
            }
            if (rarity >= UpgradeRarity.Epic && poolToDrawFrom.Any(u => u.rarity == UpgradeRarity.Rare))
            {
                return GetRandomUpgradeOfRarity(UpgradeRarity.Rare, poolToDrawFrom);
            }
            if (rarity >= UpgradeRarity.Rare && poolToDrawFrom.Any(u => u.rarity == UpgradeRarity.Common))
            {
                return GetRandomUpgradeOfRarity(UpgradeRarity.Common, poolToDrawFrom);
            }

            // As a last resort, pick any available upgrade from the remaining pool
            if (poolToDrawFrom.Count > 0)
            {
                return poolToDrawFrom[Random.Range(0, poolToDrawFrom.Count)];
            }
        }

        return null; // No upgrades available at all in the provided pool
    }


    private IEnumerator CreateUpgradeButtons(List<UpgradeData> upgrades)
    {
        foreach (GameObject button in currentUpgradeButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentUpgradeButtons.Clear();

        for (int i = 0; i < upgrades.Count; i++)
        {
            UpgradeData upgrade = upgrades[i];

            GameObject upgradeButton = Instantiate(upgradeTemplate, upgradeSelectionPanel.transform);
            upgradeButton.SetActive(true);

            RectTransform rectTransform = upgradeButton.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(500 - (i * 500), 0);

            SetupUpgradeButton(upgradeButton, upgrade);
            currentUpgradeButtons.Add(upgradeButton);

            upgradeButton.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateButtonAppear(upgradeButton));

            yield return new WaitForSeconds(animationDelay);
        }
    }

    private IEnumerator AnimateButtonAppear(GameObject button)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration && button != null)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 0.75f, elapsed / duration);
            button.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        if (button != null)
        {
            button.transform.localScale = Vector3.one * 0.75f;
        }
    }

    private void SetupUpgradeButton(GameObject buttonObj, UpgradeData upgrade)
    {
        buttonObj.SetActive(true);

        // --- BUG FIX: RAYCASTING ---
        // Ensure the main button background is the only clickable graphic.
        // Child elements like text and icons should not block clicks.
        Image backgroundImage = buttonObj.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
        }

        foreach (Graphic g in buttonObj.GetComponentsInChildren<Graphic>())
        {
            // Skip the parent button's graphic itself
            if (g.gameObject == buttonObj) continue;
            g.raycastTarget = false;
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                SelectUpgrade(upgrade);
            });
            button.interactable = true;
        }
        else
        {
            Debug.LogError("No Button component found on upgrade button!");
        }

        Text[] texts = buttonObj.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("title"))
            {
                text.text = upgrade.upgradeName;
                if (upgrade.rarityColor != Color.clear)
                    text.color = upgrade.rarityColor;
            }
            else if (text.name.ToLower().Contains("description"))
            {
                text.text = GetUpgradeDescription(upgrade);
            }
            else if (text.name.ToLower().Contains("stack"))
            {
                int currentStacks = playerUpgrades.ContainsKey(upgrade) ? playerUpgrades[upgrade] : 0;
                if (upgrade.canStack && upgrade.maxStacks > 0 && upgrade.maxStacks != 1 && currentStacks > 0)
                {
                    text.text = $"({currentStacks}/{upgrade.maxStacks})";
                }
                else
                {
                    text.text = ""; // Don't show stack count for non-stackable or unique items
                }
            }
        }

        if (backgroundImage != null && upgrade.backgroundColor != Color.clear)
        {
            backgroundImage.color = upgrade.backgroundColor;
        }

        Image[] images = buttonObj.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.name.ToLower().Contains("icon") && upgrade.upgradeIcon != null)
            {
                img.sprite = upgrade.upgradeIcon;
                img.color = Color.white;
            }
        }
    }

    private string GetUpgradeDescription(UpgradeData upgrade)
    {
        string description = upgrade.description;

        if (upgrade.canStack && upgrade.maxStacks > 1 && playerUpgrades.ContainsKey(upgrade) && playerUpgrades[upgrade] > 0)
        {
            int currentStacks = playerUpgrades[upgrade];
            description += $"\n\nCurrent Stacks: {currentStacks}";
        }

        return description;
    }

    public void SelectUpgrade(UpgradeData upgrade)
    {
        ApplyUpgrade(upgrade);
        OnUpgradeSelected?.Invoke(upgrade);
        HideUpgradeSelection();
    }

    public void SkipUpgrade()
    {
        OnUpgradeSkipped?.Invoke();
        HideUpgradeSelection();
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        if (playerUpgrades.ContainsKey(upgrade))
        {
            if (upgrade.canStack && playerUpgrades[upgrade] < upgrade.maxStacks)
            {
                playerUpgrades[upgrade]++;
            }
            else if (!upgrade.canStack || upgrade.isUnique)
            {
                Debug.LogWarning($"Attempted to apply unique/non-stackable upgrade '{upgrade.upgradeName}' again.");
                return;
            }
        }
        else
        {
            playerUpgrades[upgrade] = 1;
        }

        if (battleManager != null)
        {
            battleManager.ApplyUpgrade(upgrade, playerUpgrades[upgrade]);
        }
        else
        {
            Debug.LogError("Battle manager is null! Cannot apply upgrade.");
        }
    }

    private void HideUpgradeSelection()
    {
        if (upgradeSelectionPanel != null)
        {
            upgradeSelectionPanel.SetActive(false);
        }

        if (shadowPanel != null)
        {
            shadowPanel.SetActive(false);
        }

        OnUpgradeSelectionComplete?.Invoke();
    }

    public Dictionary<UpgradeData, int> GetPlayerUpgrades()
    {
        return new Dictionary<UpgradeData, int>(playerUpgrades);
    }

    public bool HasUpgrade(UpgradeData upgrade)
    {
        return playerUpgrades.ContainsKey(upgrade);
    }

    public int GetUpgradeStacks(UpgradeData upgrade)
    {
        return playerUpgrades.ContainsKey(upgrade) ? playerUpgrades[upgrade] : 0;
    }

    public void ResetUpgrades()
    {
        playerUpgrades.Clear();
        // Re-populate the pool if starting a new game ensures it's fresh
        PopulateAllAvailableUpgradesPool();
    }

    // --- Run Info Panel UI Methods ---

    public void OnToggleRunInfoPanel()
    {
        if (runInfoPanel == null)
        {
            Debug.LogError("Run Info Panel GameObject is not assigned! Cannot toggle.");
            return;
        }

        if (runInfoPanel.activeSelf)
        {
            HideRunInfoPanel();
        }
        else
        {
            ShowRunInfoPanel();
        }
    }

    public void ShowRunInfoPanel()
    {
        if (runInfoPanel == null)
        {
            Debug.LogError("Run Info Panel GameObject is not assigned!");
            return;
        }

        // --- BUG FIX: UI OVERLAP ---
        // Hide the enemy health bars when showing the run info panel.
        if (battleManager != null && battleManager.uiManager != null)
        {
            battleManager.uiManager.SetEnemyUIPanelActive(false);
        }

        runInfoPanel.SetActive(true);
        PopulateRunInfo();
    }

    public void HideRunInfoPanel()
    {
        if (runInfoPanel != null)
        {
            // --- BUG FIX: UI OVERLAP ---
            // Re-show the enemy health bars when hiding the run info panel.
            if (battleManager != null && battleManager.uiManager != null)
            {
                battleManager.uiManager.SetEnemyUIPanelActive(true);
            }

            runInfoPanel.SetActive(false);
            ClearRunInfoContainers();
        }
    }

    private void PopulateRunInfo()
    {
        ClearRunInfoContainers();

        if (runInfoQuestionText != null)
        {
            runInfoQuestionText.text = "RUN INFO";
        }

        float yOffsetIncrement = -40f; // This is the increment for both boons and abilities

        if (boonsContainer != null && runInfoUpgradeTextTemplate != null)
        {
            float currentYOffsetBoons = 200f; // Starting Y position for boons

            foreach (var entry in playerUpgrades)
            {
                UpgradeData upgrade = entry.Key;
                int stacks = entry.Value;

                if (runInfoUpgradeTextTemplate.GetComponent<Text>() == null)
                {
                    Debug.LogError("Run Info Upgrade Text Template prefab is missing a Text component!");
                    return;
                }

                GameObject upgradeEntry = Instantiate(runInfoUpgradeTextTemplate, boonsContainer);
                Text text = upgradeEntry.GetComponent<Text>();
                text.enabled = true;

                RectTransform rectTransform = upgradeEntry.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentYOffsetBoons);
                    currentYOffsetBoons += yOffsetIncrement;
                }

                Text upgradeText = upgradeEntry.GetComponent<Text>();
                if (upgradeText != null)
                {
                    string stackText = "";
                    if (upgrade.canStack && upgrade.maxStacks > 1)
                    {
                        stackText = $" (x{stacks})";
                    }
                    upgradeText.text = $"{upgrade.upgradeName}{stackText}";

                    if (upgrade.rarityColor != Color.clear)
                    {
                        upgradeText.color = upgrade.rarityColor;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Boons Container or Run Info Upgrade Text Template is not assigned for boons. Cannot populate.");
        }

        if (abilitiesContainer != null && runInfoUpgradeTextTemplate != null)
        {
            float currentYOffsetAbilities = 200f;

            if (battleManager != null)
            {
                List<AttackData> currentAbilities = battleManager.GetCurrentPlayerAttacks();

                foreach (AttackData attack in currentAbilities)
                {
                    GameObject abilityEntry = Instantiate(runInfoUpgradeTextTemplate, abilitiesContainer);
                    Text abilityText = abilityEntry.GetComponent<Text>();

                    RectTransform rectTransform = abilityEntry.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentYOffsetAbilities);
                        currentYOffsetAbilities += yOffsetIncrement;
                    }

                    if (abilityText != null)
                    {
                        abilityText.text = attack.attackName;
                        abilityText.color = StoryAttackDataManager.Instance.GetColorForAttackType(attack.attackType);
                    }
                }
            }
            else
            {
                Debug.LogWarning("BattleManager is null. Cannot retrieve current player attacks for abilities.");
            }
        }
        else
        {
            Debug.LogWarning("Abilities Container or Run Info Upgrade Text Template is not assigned for abilities. Cannot populate.");
        }
    }

    private void ClearRunInfoContainers()
    {
        if (boonsContainer != null)
        {
            foreach (Transform child in boonsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (abilitiesContainer != null)
        {
            foreach (Transform child in abilitiesContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}