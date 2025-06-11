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
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> commonUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> rareUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> epicUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> legendaryUpgrades = new List<UpgradeData>();

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

        // Initialize Run Info Panel as hidden
        if (runInfoPanel != null)
            runInfoPanel.SetActive(false);

        SetupSkipButton();
        CategorizeUpgrades();
        SetupDefaultCurves();
    }

    private void SetupSkipButton()
    {
        if (skipUpgradeButton != null)
        {
            skipUpgradeButton.onClick.AddListener(SkipUpgrade);
            skipUpgradeButton.gameObject.SetActive(allowSkipUpgrade);
        }
    }

    private void CategorizeUpgrades()
    {
        commonUpgrades.Clear();
        rareUpgrades.Clear();
        epicUpgrades.Clear();
        legendaryUpgrades.Clear();

        foreach (UpgradeData upgrade in allUpgrades)
        {
            switch (upgrade.rarity)
            {
                case UpgradeRarity.Common:
                    commonUpgrades.Add(upgrade);
                    break;
                case UpgradeRarity.Rare:
                    rareUpgrades.Add(upgrade);
                    break;
                case UpgradeRarity.Epic:
                    epicUpgrades.Add(upgrade);
                    break;
                case UpgradeRarity.Legendary:
                    legendaryUpgrades.Add(upgrade);
                    break;
            }
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

    public void ShowUpgradeSelection(int currentWave)
    {
        if (upgradeSelectionPanel == null) return;

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
        List<UpgradeData> availableUpgrades = GetAvailableUpgrades();

        for (int i = 0; i < upgradeChoicesCount; i++)
        {
            if (availableUpgrades.Count == 0) break;

            UpgradeRarity selectedRarity = GetRandomRarity(currentWave);
            UpgradeData selectedUpgrade = GetRandomUpgradeOfRarity(selectedRarity, availableUpgrades);

            if (selectedUpgrade != null)
            {
                selectedUpgrades.Add(selectedUpgrade);
                availableUpgrades.Remove(selectedUpgrade);
            }
        }

        while (selectedUpgrades.Count < upgradeChoicesCount && allUpgrades.Count > 0)
        {
            UpgradeData fallbackUpgrade = allUpgrades[Random.Range(0, allUpgrades.Count)];
            if (!selectedUpgrades.Contains(fallbackUpgrade))
            {
                selectedUpgrades.Add(fallbackUpgrade);
            }
            else
            {
                var potentialUpgrade = allUpgrades.FirstOrDefault(u => !selectedUpgrades.Contains(u));
                if (potentialUpgrade != null) selectedUpgrades.Add(potentialUpgrade);
                else break;
            }
        }

        return selectedUpgrades;
    }

    private List<UpgradeData> GetAvailableUpgrades()
    {
        List<UpgradeData> available = new List<UpgradeData>();

        foreach (UpgradeData upgrade in allUpgrades)
        {
            if (upgrade.isUnique && playerUpgrades.ContainsKey(upgrade))
                continue;

            if (playerUpgrades.ContainsKey(upgrade) &&
                playerUpgrades[upgrade] >= upgrade.maxStacks)
                continue;

            available.Add(upgrade);
        }

        return available;
    }

    private UpgradeRarity GetRandomRarity(int currentWave)
    {
        float commonChance = commonChanceCurve.Evaluate(currentWave);
        float rareChance = rareChanceCurve.Evaluate(currentWave);
        float epicChance = epicChanceCurve.Evaluate(currentWave);
        float legendaryChance = legendaryChanceCurve.Evaluate(currentWave);

        float totalChance = commonChance + rareChance + epicChance + legendaryChance;
        float randomValue = Random.Range(0f, totalChance);

        if (randomValue < legendaryChance && legendaryUpgrades.Count > 0)
            return UpgradeRarity.Legendary;
        else if (randomValue < legendaryChance + epicChance && epicUpgrades.Count > 0)
            return UpgradeRarity.Epic;
        else if (randomValue < legendaryChance + epicChance + rareChance && rareUpgrades.Count > 0)
            return UpgradeRarity.Rare;
        else
            return UpgradeRarity.Common;
    }

    private UpgradeData GetRandomUpgradeOfRarity(UpgradeRarity rarity, List<UpgradeData> availableUpgrades)
    {
        List<UpgradeData> rarityUpgrades = availableUpgrades.Where(u => u.rarity == rarity).ToList();

        if (rarityUpgrades.Count == 0)
        {
            if (rarity == UpgradeRarity.Legendary)
            {
                rarityUpgrades = availableUpgrades.Where(u => u.rarity == UpgradeRarity.Epic).ToList();
            }
            if (rarityUpgrades.Count == 0 && rarity >= UpgradeRarity.Epic)
            {
                rarityUpgrades = availableUpgrades.Where(u => u.rarity == UpgradeRarity.Rare).ToList();
            }
            if (rarityUpgrades.Count == 0 && rarity >= UpgradeRarity.Rare)
            {
                rarityUpgrades = availableUpgrades.Where(u => u.rarity == UpgradeRarity.Common).ToList();
            }
        }

        if (rarityUpgrades.Count == 0)
        {
            rarityUpgrades = availableUpgrades;
        }

        if (rarityUpgrades.Count == 0)
        {
            return null;
        }

        return rarityUpgrades[Random.Range(0, rarityUpgrades.Count)];
    }

    private IEnumerator CreateUpgradeButtons(List<UpgradeData> upgrades)
    {
        // Clear any existing buttons first
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
            upgradeButton.SetActive(true); // Ensure the button is active

            // Manual positioning logic
            RectTransform rectTransform = upgradeButton.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(500 - (i * 500), 0);

            // Setup the button BEFORE adding to list and animating
            SetupUpgradeButton(upgradeButton, upgrade);
            currentUpgradeButtons.Add(upgradeButton);

            // Animate button appearance
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
        // Make sure the button GameObject is active
        buttonObj.SetActive(true);

        // Setup button click listener
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                SelectUpgrade(upgrade);
            });

            // Ensure the button is interactable
            button.interactable = true;
        }
        else
        {
            Debug.LogError("No Button component found on upgrade button!");
        }

        // Setup text components
        Text[] texts = buttonObj.GetComponentsInChildren<Text>(true); // Include inactive children
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
                if (upgrade.canStack && currentStacks > 0)
                {
                    text.text = $"({currentStacks}/{upgrade.maxStacks})";
                }
                else
                {
                    text.text = "";
                }
            }
        }

        // Setup background image
        Image backgroundImage = buttonObj.GetComponent<Image>();
        if (backgroundImage != null && upgrade.backgroundColor != Color.clear)
        {
            backgroundImage.color = upgrade.backgroundColor;
        }

        // Setup icon image
        Image[] images = buttonObj.GetComponentsInChildren<Image>(true); // Include inactive children
        foreach (Image img in images)
        {
            if (img.name.ToLower().Contains("icon") && upgrade.upgradeIcon != null)
            {
                img.sprite = upgrade.upgradeIcon;
                img.color = Color.white;
            }
        }

        // Ensure all Canvas Group components (if any) are set to interactable
        CanvasGroup[] canvasGroups = buttonObj.GetComponentsInChildren<CanvasGroup>();
        foreach (CanvasGroup cg in canvasGroups)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private string GetUpgradeDescription(UpgradeData upgrade)
    {
        string description = upgrade.description;

        if (upgrade.canStack && playerUpgrades.ContainsKey(upgrade))
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
            playerUpgrades[upgrade]++;
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
    }

    // --- Run Info Panel UI Methods ---

    // This method can be called from a UI Button or other game logic to toggle the panel
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
        runInfoPanel.SetActive(true);
        PopulateRunInfo();
    }

    public void HideRunInfoPanel()
    {
        if (runInfoPanel != null)
        {
            runInfoPanel.SetActive(false);
            ClearRunInfoContainers();
        }
    }

    private void PopulateRunInfo()
    {
        ClearRunInfoContainers(); // Clear previous entries

        // Set the main title text
        if (runInfoQuestionText != null)
        {
            runInfoQuestionText.text = "RUN INFO";
        }

        // Populate Boons (Upgrades)
        if (boonsContainer != null && runInfoUpgradeTextTemplate != null)
        {
            float currentYOffset = 200f;
            float yOffsetIncrement = -40f;

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
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentYOffset);
                    currentYOffset += yOffsetIncrement; // Update offset for the next text
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

        // Abilities Section (Placeholder)
        if (abilitiesContainer != null && runInfoUpgradeTextTemplate != null)
        {
            // Add any ability entries here if you have them.
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