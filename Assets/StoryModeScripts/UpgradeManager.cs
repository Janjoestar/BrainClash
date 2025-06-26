using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject upgradeSelectionPanel;
    [SerializeField] private GameObject shadowPanel;
    [SerializeField] private GameObject upgradeTemplate;
    [SerializeField] private Text waveCompleteText;
    [SerializeField] private Button skipUpgradeButton;

    [Header("Run Info UI References")]
    [SerializeField] private GameObject runInfoPanel;
    [SerializeField] private Text runInfoQuestionText;
    [SerializeField] private Transform boonsContainer;
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject runInfoUpgradeTextTemplate;

    [Header("Upgrade Database")]
    [SerializeField] private List<UpgradeData> staticCommonUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> staticRareUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> staticEpicUpades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> staticLegendaryUpgrades = new List<UpgradeData>();
    private List<UpgradeData> allDynamicAttackUpgrades = new List<UpgradeData>();
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

    // Callbacks for when an upgrade is selected, skipped, or the entire process is complete.
    public System.Action<UpgradeData> OnUpgradeSelected;
    public System.Action OnUpgradeSkipped;
    public System.Action OnUpgradeSelectionComplete;

    private Dictionary<UpgradeData, int> playerUpgrades = new Dictionary<UpgradeData, int>();
    private List<GameObject> currentUpgradeButtons = new List<GameObject>();
    private StoryBattleManager battleManager;

    #region Unity Lifecycle
    private void Awake()
    {
        battleManager = FindObjectOfType<StoryBattleManager>();

        if (upgradeSelectionPanel != null) upgradeSelectionPanel.SetActive(false);
        if (shadowPanel != null) shadowPanel.SetActive(false);
        if (runInfoPanel != null) runInfoPanel.SetActive(false);

        SetupSkipButton();
        SetupDefaultCurves();
        InitializeDynamicUpgrades();
        PopulateAllAvailableUpgradesPool();
    }
    #endregion

    #region Public API
    public void ShowUpgradeSelection(int currentWave)
    {
        if (upgradeSelectionPanel == null) return;
        if (battleManager != null) battleManager.HideAttackHover();

        ClearCurrentUpgrades();
        List<UpgradeData> selectedUpgrades = GenerateRandomUpgrades(currentWave);

        upgradeSelectionPanel.SetActive(true);
        shadowPanel.SetActive(true);

        if (waveCompleteText != null)
        {
            waveCompleteText.text = $"Wave {currentWave} Complete!\nChoose an Upgrade:";
        }

        StartCoroutine(CreateUpgradeButtons(selectedUpgrades));
    }

    public void SelectUpgrade(UpgradeData upgrade)
    {
        // Hide the upgrade cards immediately, but keep the shadow panel active.
        upgradeSelectionPanel.SetActive(false);

        if (upgrade.upgradeType != UpgradeType.AttackModification)
        {
            // Apply stat/new attack upgrades directly and finish the process.
            ApplyAndFinalize(upgrade);
        }
        else
        {
            // For attack mods, defer to the BattleManager to handle the choice.
            // The shadow panel remains active to block UI until a choice is made.
            battleManager.EnterAttackSelectionModeForUpgrade(upgrade);
        }
    }

    /// <summary>
    /// Called by StoryBattleManager AFTER the player has chosen an attack to modify.
    /// </summary>
    public void FinalizeUpgradeSelection(UpgradeData appliedUpgrade)
    {
        ApplyUpgrade(appliedUpgrade);
        OnUpgradeSelected?.Invoke(appliedUpgrade);
        HideUpgradeSelection(); // Hides shadow panel and starts the next wave.
    }

    public void SkipUpgrade()
    {
        OnUpgradeSkipped?.Invoke();
        HideUpgradeSelection();
    }
    #endregion

    #region Upgrade Application
    private void ApplyAndFinalize(UpgradeData upgrade)
    {
        ApplyUpgrade(upgrade);
        OnUpgradeSelected?.Invoke(upgrade);
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
        }
        else
        {
            playerUpgrades[upgrade] = 1;
        }

        // Apply stat-based upgrades to the battle manager. Attack mods are handled by the BattleManager itself.
        if (battleManager != null && upgrade.upgradeType != UpgradeType.AttackModification)
        {
            int stacks = playerUpgrades.ContainsKey(upgrade) ? playerUpgrades[upgrade] : 1;
            battleManager.ApplyUpgrade(upgrade, stacks);
        }
    }
    #endregion

    #region UI Management
    private void HideUpgradeSelection()
    {
        if (upgradeSelectionPanel != null) upgradeSelectionPanel.SetActive(false);
        if (shadowPanel != null) shadowPanel.SetActive(false);

        OnUpgradeSelectionComplete?.Invoke();
    }

    private IEnumerator CreateUpgradeButtons(List<UpgradeData> upgrades)
    {
        ClearCurrentUpgrades();

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
            button.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        if (button != null) button.transform.localScale = Vector3.one * 0.75f;
    }

    private void SetupUpgradeButton(GameObject buttonObj, UpgradeData upgrade)
    {
        buttonObj.SetActive(true);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectUpgrade(upgrade));
            button.interactable = true;
        }

        foreach (Text text in buttonObj.GetComponentsInChildren<Text>(true))
        {
            if (text.name.ToLower().Contains("name"))
            {
                text.text = upgrade.upgradeName;
                if (upgrade.rarityColor != Color.clear) text.color = upgrade.rarityColor;
            }
            else if (text.name.ToLower().Contains("description"))
            {
                text.text = GetUpgradeDescription(upgrade);
            }
            else if (text.name.ToLower().Contains("stack"))
            {
                int currentStacks = playerUpgrades.ContainsKey(upgrade) ? playerUpgrades[upgrade] : 0;
                text.text = (upgrade.canStack && upgrade.maxStacks > 1 && currentStacks > 0)
                    ? $"({currentStacks}/{upgrade.maxStacks})"
                    : "";
            }
        }

        Image backgroundImage = buttonObj.GetComponent<Image>();
        if (backgroundImage != null)
        {
            if (upgrade.backgroundColor != Color.clear) backgroundImage.color = upgrade.backgroundColor;
            backgroundImage.raycastTarget = true;
        }

        foreach (Image img in buttonObj.GetComponentsInChildren<Image>(true))
        {
            img.raycastTarget = (img.gameObject == buttonObj); // Only button background is clickable
            if (img.name.ToLower().Contains("icon") && upgrade.upgradeIcon != null)
            {
                img.sprite = upgrade.upgradeIcon;
                img.color = Color.white;
            }
        }
    }
    #endregion

    #region Upgrade Generation
    private List<UpgradeData> GenerateRandomUpgrades(int currentWave)
    {
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();
        List<UpgradeData> availableUpgradesPool = GetAvailableUpgradesFromPool(allAvailableUpgradesPool);

        bool isAbilityWave = (currentWave > 0 && currentWave % 2 == 0);
        if (isAbilityWave)
        {
            List<UpgradeData> availableAttackUpgrades = GetAvailableUpgradesFromPool(allDynamicAttackUpgrades);
            availableUpgradesPool.AddRange(availableAttackUpgrades);
        }

        List<UpgradeData> tempWorkingList = new List<UpgradeData>(availableUpgradesPool);

        for (int i = 0; i < upgradeChoicesCount; i++)
        {
            if (tempWorkingList.Count == 0) break;

            UpgradeRarity desiredRarity = GetRandomRarity(currentWave, tempWorkingList);
            UpgradeData selectedUpgrade = GetRandomUpgradeOfRarity(desiredRarity, tempWorkingList);

            if (selectedUpgrade != null)
            {
                selectedUpgrades.Add(selectedUpgrade);
                tempWorkingList.Remove(selectedUpgrade);
            }
            else if (tempWorkingList.Any())
            {
                UpgradeData fallback = tempWorkingList[Random.Range(0, tempWorkingList.Count)];
                selectedUpgrades.Add(fallback);
                tempWorkingList.Remove(fallback);
            }
        }
        return selectedUpgrades;
    }

    private UpgradeRarity GetRandomRarity(int currentWave, List<UpgradeData> currentPool)
    {
        float commonChance = commonChanceCurve.Evaluate(currentWave);
        float rareChance = rareChanceCurve.Evaluate(currentWave);
        float epicChance = epicChanceCurve.Evaluate(currentWave);
        float legendaryChance = legendaryChanceCurve.Evaluate(currentWave);
        float totalChance = commonChance + rareChance + epicChance + legendaryChance;

        float randomValue = Random.Range(0f, totalChance);

        if (randomValue < legendaryChance && currentPool.Any(u => u.rarity == UpgradeRarity.Legendary)) return UpgradeRarity.Legendary;
        randomValue -= legendaryChance;
        if (randomValue < epicChance && currentPool.Any(u => u.rarity == UpgradeRarity.Epic)) return UpgradeRarity.Epic;
        randomValue -= epicChance;
        if (randomValue < rareChance && currentPool.Any(u => u.rarity == UpgradeRarity.Rare)) return UpgradeRarity.Rare;

        if (currentPool.Any(u => u.rarity == UpgradeRarity.Common)) return UpgradeRarity.Common;
        if (currentPool.Any(u => u.rarity == UpgradeRarity.Rare)) return UpgradeRarity.Rare;
        if (currentPool.Any(u => u.rarity == UpgradeRarity.Epic)) return UpgradeRarity.Epic;
        return UpgradeRarity.Legendary;
    }

    private UpgradeData GetRandomUpgradeOfRarity(UpgradeRarity rarity, List<UpgradeData> pool)
    {
        List<UpgradeData> raritySpecificUpgrades = pool.Where(u => u.rarity == rarity).ToList();
        if (raritySpecificUpgrades.Any())
        {
            return raritySpecificUpgrades[Random.Range(0, raritySpecificUpgrades.Count)];
        }

        if (rarity == UpgradeRarity.Legendary) return GetRandomUpgradeOfRarity(UpgradeRarity.Epic, pool);
        if (rarity == UpgradeRarity.Epic) return GetRandomUpgradeOfRarity(UpgradeRarity.Rare, pool);
        if (rarity == UpgradeRarity.Rare) return GetRandomUpgradeOfRarity(UpgradeRarity.Common, pool);

        return pool.FirstOrDefault();
    }
    #endregion

    #region Helper & Setup Methods
    private List<UpgradeData> GetAvailableUpgradesFromPool(List<UpgradeData> pool)
    {
        return pool.Where(upgrade =>
            !(upgrade.isUnique && playerUpgrades.ContainsKey(upgrade)) &&
            !(playerUpgrades.ContainsKey(upgrade) && playerUpgrades[upgrade] >= upgrade.maxStacks)
        ).ToList();
    }

    private void ClearCurrentUpgrades()
    {
        foreach (GameObject button in currentUpgradeButtons)
        {
            if (button != null) Destroy(button);
        }
        currentUpgradeButtons.Clear();
    }

    private void PopulateAllAvailableUpgradesPool()
    {
        allAvailableUpgradesPool.Clear();
        allAvailableUpgradesPool.AddRange(staticCommonUpgrades);
        allAvailableUpgradesPool.AddRange(staticRareUpgrades);
        allAvailableUpgradesPool.AddRange(staticEpicUpades);
        allAvailableUpgradesPool.AddRange(staticLegendaryUpgrades);
    }

    public void ResetUpgrades()
    {
        playerUpgrades.Clear();
        PopulateAllAvailableUpgradesPool();
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
        if (commonChanceCurve.keys.Length == 0) commonChanceCurve = AnimationCurve.Linear(1, 0.7f, 5, 0.3f);
        if (rareChanceCurve.keys.Length == 0) rareChanceCurve = AnimationCurve.Linear(1, 0.25f, 5, 0.4f);
        if (epicChanceCurve.keys.Length == 0) epicChanceCurve = AnimationCurve.Linear(1, 0.05f, 5, 0.25f);
        if (legendaryChanceCurve.keys.Length == 0) legendaryChanceCurve = AnimationCurve.Linear(1, 0.0f, 5, 0.05f);
    }

    private void InitializeDynamicUpgrades()
    {
        allDynamicAttackUpgrades.Clear();
        List<string> characterNames = new List<string> { "Knight", "Archer", "Water", "Fire", "Wind", "Necromancer", "Crystal", "Ground" };

        foreach (string charName in characterNames)
        {
            List<AttackData> unlockableAttacks = StoryAttackDataManager.Instance.GetUnlockableAttacksForCharacter(charName);
            foreach (AttackData attack in unlockableAttacks)
            {
                UpgradeData attackUpgrade = ScriptableObject.CreateInstance<UpgradeData>();
                attackUpgrade.upgradeName = $"Unlock {attack.attackName}";
                attackUpgrade.description = $"Grants the '{attack.attackName}' attack: {attack.description}";
                attackUpgrade.upgradeType = UpgradeType.SpecialAttack;
                attackUpgrade.rarity = GetRarityForAttack(attack.attackName);
                attackUpgrade.grantsNewAttack = true;
                attackUpgrade.newAttackName = attack.attackName;
                attackUpgrade.canStack = false;
                attackUpgrade.isUnique = true;
                attackUpgrade.SetRarityColorsInternal();
                allDynamicAttackUpgrades.Add(attackUpgrade);
            }
        }
    }

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

    private string GetUpgradeDescription(UpgradeData upgrade)
    {
        string description = upgrade.description;
        if (upgrade.canStack && upgrade.maxStacks > 1 && playerUpgrades.ContainsKey(upgrade) && playerUpgrades[upgrade] > 0)
        {
            description += $"\n\nCurrent Stacks: {playerUpgrades[upgrade]}";
        }
        return description;
    }

    public Dictionary<UpgradeData, int> GetPlayerUpgrades() => new Dictionary<UpgradeData, int>(playerUpgrades);
    public bool HasUpgrade(UpgradeData upgrade) => playerUpgrades.ContainsKey(upgrade);
    public int GetUpgradeStacks(UpgradeData upgrade) => playerUpgrades.ContainsKey(upgrade) ? playerUpgrades[upgrade] : 0;
    #endregion

    #region Run Info Panel
    public void OnToggleRunInfoPanel()
    {
        if (runInfoPanel == null) return;

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
        if (runInfoPanel == null) return;
        if (battleManager?.uiManager != null) battleManager.uiManager.SetEnemyUIPanelActive(false);

        runInfoPanel.SetActive(true);
        PopulateRunInfo();
    }

    public void HideRunInfoPanel()
    {
        if (runInfoPanel == null) return;
        if (battleManager?.uiManager != null) battleManager.uiManager.SetEnemyUIPanelActive(true);

        runInfoPanel.SetActive(false);
        ClearRunInfoContainers();
    }

    private void PopulateRunInfo()
    {
        ClearRunInfoContainers();
        if (runInfoQuestionText != null) runInfoQuestionText.text = "RUN INFO";

        float yOffsetIncrement = -40f;

        // Populate Boons (general upgrades)
        if (boonsContainer != null && runInfoUpgradeTextTemplate != null)
        {
            float currentYOffset = 200f;
            foreach (var entry in playerUpgrades)
            {
                UpgradeData upgrade = entry.Key;
                int stacks = entry.Value;

                GameObject entryObj = Instantiate(runInfoUpgradeTextTemplate, boonsContainer);
                Text text = entryObj.GetComponent<Text>();
                if (text == null) continue;

                text.enabled = true;
                string stackText = (upgrade.canStack && upgrade.maxStacks > 1) ? $" (x{stacks})" : "";
                text.text = $"{upgrade.upgradeName}{stackText}";
                if (upgrade.rarityColor != Color.clear) text.color = upgrade.rarityColor;

                RectTransform rectTransform = entryObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentYOffset);
                    currentYOffset += yOffsetIncrement;
                }
            }
        }

        // Populate Abilities (attacks)
        if (abilitiesContainer != null && runInfoUpgradeTextTemplate != null && battleManager != null)
        {
            float currentYOffset = 200f;
            List<AttackData> currentAbilities = battleManager.GetCurrentPlayerAttacks();
            foreach (AttackData attack in currentAbilities)
            {
                GameObject entryObj = Instantiate(runInfoUpgradeTextTemplate, abilitiesContainer);
                Text text = entryObj.GetComponent<Text>();
                if (text == null) continue;

                text.text = attack.attackName;
                text.color = StoryAttackDataManager.Instance.GetColorForAttackType(attack.attackType);

                RectTransform rectTransform = entryObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentYOffset);
                    currentYOffset += yOffsetIncrement;
                }
            }
        }
    }

    private void ClearRunInfoContainers()
    {
        if (boonsContainer != null)
        {
            foreach (Transform child in boonsContainer) Destroy(child.gameObject);
        }
        if (abilitiesContainer != null)
        {
            foreach (Transform child in abilitiesContainer) Destroy(child.gameObject);
        }
    }
    #endregion
}