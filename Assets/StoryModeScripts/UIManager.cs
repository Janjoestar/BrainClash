using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class UIManager
{
    private MonoBehaviour monoBehaviourInstance;
    private GameObject battlePanel;
    private GameObject attackPanel;
    private GameObject enemySelectionPanel;
    private GameObject endScreenPanel;
    private GameObject enemyUIPanel;

    private Text playerHealthText;
    private Text battleStatusText;
    private Text resultText;
    private Button returnToMenuButton;
    private Button continueButton;
    private List<Button> enemySelectionButtons = new List<Button>();
    private Text waveReachedText;

    private GameObject attackButtonPrefab;
    private GameObject attackHoverPrefab;
    private GameObject enemyUIElementPrefab;
    private float hoverDelay;
    private Vector3 enemyUIOffset;
    private Func<string[]> getEnemyNames;

    private List<GameObject> enemyUIInstances = new List<GameObject>();

    public UIManager(MonoBehaviour monoBehaviourInstance, GameObject battlePanel, GameObject attackPanel, GameObject enemySelectionPanel, GameObject endScreenPanel, GameObject enemyUIPanel,
                     Text playerHealthText, Text battleStatusText, Text resultText, Button returnToMenuButton, Button continueButton,
                     Transform enemySelectionButtonsParent,
                     GameObject attackButtonPrefab, GameObject attackHoverPrefab, GameObject enemyUIElementPrefab,
                     float hoverDelay, Vector3 enemyUIOffset, Text waveReachedText, Func<string[]> getEnemyNames)
    {
        this.monoBehaviourInstance = monoBehaviourInstance;
        this.battlePanel = battlePanel;
        this.attackPanel = attackPanel;
        this.enemySelectionPanel = enemySelectionPanel;
        this.endScreenPanel = endScreenPanel;
        this.enemyUIPanel = enemyUIPanel;
        this.playerHealthText = playerHealthText;
        this.battleStatusText = battleStatusText;
        this.resultText = resultText;
        this.returnToMenuButton = returnToMenuButton;
        this.continueButton = continueButton;
        if (enemySelectionButtonsParent != null)
        {
            foreach (Transform child in enemySelectionButtonsParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    enemySelectionButtons.Add(button);
                }
            }
        }

        this.attackButtonPrefab = attackButtonPrefab;
        this.attackHoverPrefab = attackHoverPrefab;
        this.enemyUIElementPrefab = enemyUIElementPrefab;
        this.hoverDelay = hoverDelay;
        this.enemyUIOffset = enemyUIOffset;
        this.waveReachedText = waveReachedText;
        this.getEnemyNames = getEnemyNames;
    }

    public void UpdatePlayerHealth(float health)
    {
        playerHealthText.text = "Player HP: " + Mathf.Round(health);
    }

    public void UpdateEnemyHealths(List<float> currentEnemyHealths)
    {
        for (int i = 0; i < enemyUIInstances.Count && i < currentEnemyHealths.Count; i++)
        {
            if (enemyUIInstances[i] != null)
            {
                Text[] texts = enemyUIInstances[i].GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.Contains("Health"))
                    {
                        text.text = $"HP: {Mathf.Round(currentEnemyHealths[i])}";
                    }
                }
                enemyUIInstances[i].SetActive(currentEnemyHealths[i] > 0);
            }
        }
    }

    public void UpdateBattleStatus(string status)
    {
        battleStatusText.text = status;
    }

    public void SetEnemySelectionPanelActive(bool active)
    {
        if (enemySelectionPanel != null)
            enemySelectionPanel.SetActive(active);
    }

    public void SetAttackPanelActive(bool active)
    {
        if (attackPanel != null)
            attackPanel.SetActive(active);
    }

    // In UIManager.cs

    public void CreateEnemyUI(List<GameObject> enemies, List<float> currentEnemyHealths)
    {
        if (enemyUIPanel == null) return;

        ClearEnemyUI();

        // Get the parent canvas once to check its render mode
        Canvas parentCanvas = enemyUIPanel.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("EnemyUIPanel is not placed under a Canvas!");
            return;
        }

        // Determine the correct camera for coordinate conversion. For Overlay, it's null.
        Camera uiCamera = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemyUIElementPrefab != null && enemies[i] != null)
            {
                GameObject uiInstance = GameObject.Instantiate(enemyUIElementPrefab, enemyUIPanel.transform);
                RectTransform uiRect = uiInstance.GetComponent<RectTransform>();

                // --- THIS IS THE CORRECTED LOGIC ---
                if (uiRect != null)
                {
                    // 1. Convert world position to a screen point (same as before)
                    Vector3 enemyWorldPos = enemies[i].transform.position + enemyUIOffset;
                    Vector2 screenPoint = Camera.main.WorldToScreenPoint(enemyWorldPos);

                    // 2. Convert the screen point to a local point on the canvas, using our determined uiCamera
                    RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint);

                    // 3. Set the anchoredPosition for reliable placement
                    uiRect.anchoredPosition = localPoint;
                }

                // The rest of the logic remains the same
                Text[] texts = uiInstance.GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.Contains("Name"))
                    {
                        string[] names = getEnemyNames();
                        text.text = (i < names.Length) ? names[i] : "Enemy " + (i + 1);
                    }
                    else if (text.name.Contains("Health"))
                    {
                        text.text = $"HP: {Mathf.Round(currentEnemyHealths[i])}";
                    }
                }
                enemyUIInstances.Add(uiInstance);
            }
        }
    }

    public void ClearEnemyUI()
    {
        foreach (GameObject uiInstance in enemyUIInstances)
        {
            if (uiInstance != null)
                GameObject.Destroy(uiInstance);
        }
        enemyUIInstances.Clear();
    }

    public void SetupEnemySelectionButtons(List<int> aliveEnemies, string[] allSpawnedEnemyNames, Action<int> onEnemySelectedCallback)
    {
        Dictionary<string, int> currentDuplicateCounts = new Dictionary<string, int>();
        Dictionary<string, int> totalAliveNameOccurrences = new Dictionary<string, int>();
        foreach (int enemyActualIndex in aliveEnemies)
        {
            string baseName = allSpawnedEnemyNames[enemyActualIndex];
            if (!totalAliveNameOccurrences.ContainsKey(baseName))
            {
                totalAliveNameOccurrences[baseName] = 0;
            }
            totalAliveNameOccurrences[baseName]++;
        }

        for (int i = 0; i < enemySelectionButtons.Count; i++)
        {
            if (i < aliveEnemies.Count && enemySelectionButtons[i] != null)
            {
                int enemyActualIndex = aliveEnemies[i];
                enemySelectionButtons[i].gameObject.SetActive(true);

                // Set button text (same logic as before)
                Text buttonText = enemySelectionButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    string baseName = allSpawnedEnemyNames[enemyActualIndex];
                    string displayName = baseName;
                    if (totalAliveNameOccurrences.ContainsKey(baseName) && totalAliveNameOccurrences[baseName] > 1)
                    {
                        if (!currentDuplicateCounts.ContainsKey(baseName)) currentDuplicateCounts[baseName] = 0;
                        currentDuplicateCounts[baseName]++;
                        displayName = $"{baseName} ({currentDuplicateCounts[baseName]})";
                    }
                    buttonText.text = displayName;
                }

                Button button = enemySelectionButtons[i].GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onEnemySelectedCallback(enemyActualIndex));

                // --- THIS IS THE NEW PART ---
                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                // Add PointerEnter event
                EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                pointerEnter.callback.AddListener((eventData) => { ((StoryBattleManager)monoBehaviourInstance).OnEnemyButtonHoverEnter(enemyActualIndex); });
                trigger.triggers.Add(pointerEnter);

                // Add PointerExit event
                EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                pointerExit.callback.AddListener((eventData) => { ((StoryBattleManager)monoBehaviourInstance).OnEnemyButtonHoverExit(enemyActualIndex); });
                trigger.triggers.Add(pointerExit);
            }
            else if (enemySelectionButtons[i] != null)
            {
                enemySelectionButtons[i].gameObject.SetActive(false);
            }
        }
    }


    public void ShowEndScreen(bool playerWon, int currentWave, int maxWaves)
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

            if (waveReachedText != null)
            {
                waveReachedText.text = "Wave Reached: " + currentWave;
            }
        }
    }

    public void SetupEndScreenButtons(bool playerWon, Action returnToMenuCallback, Action continueCallback)
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(() => returnToMenuCallback());
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continueCallback());
        }
    }

    public void SetAttackButtonsInteractable(bool interactable)
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

    public void ClearAttackButtons()
    {
        foreach (Transform child in attackPanel.transform)
        {
            if (child.gameObject != attackHoverPrefab && child.gameObject.GetComponent<Button>() != null)
                GameObject.Destroy(child.gameObject);
        }
    }

    public void CreateAttackButtons(List<AttackData> playerAttacks, Action<AttackData> onAttackSelectedCallback,
                                     Action<AttackData, RectTransform> onHoverEnterCallback, Action onHoverExitCallback)
    {
        float startY = -75;
        float xPosition = 692;
        float yStep = -100;

        for (int i = 0; i < playerAttacks.Count; i++)
        {
            AttackData attack = playerAttacks[i];
            GameObject buttonObj = GameObject.Instantiate(attackButtonPrefab, attackPanel.transform);

            SetupAttackButton(buttonObj, attack, onAttackSelectedCallback);
            PositionAttackButton(buttonObj, xPosition, startY + (yStep * i));
            AddHoverToButton(buttonObj, attack, buttonObj.GetComponent<RectTransform>(), onHoverEnterCallback, onHoverExitCallback);

            buttonObj.SetActive(true);
        }
    }

    private void SetupAttackButton(GameObject buttonObj, AttackData attack, Action<AttackData> onAttackSelectedCallback)
    {
        Button button = buttonObj.GetComponent<Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        if (buttonText != null)
        {
            buttonText.text = attack.attackName;
        }

        button.onClick.AddListener(() => onAttackSelectedCallback(attack));
    }

    private void PositionAttackButton(GameObject buttonObj, float x, float y)
    {
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(x, y);
    }

    private void AddHoverToButton(GameObject buttonObj, AttackData attack, RectTransform buttonRect, Action<AttackData, RectTransform> onHoverEnterCallback, Action onHoverExitCallback)
    {
        EventTrigger eventTrigger = buttonObj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = buttonObj.AddComponent<EventTrigger>();
        }

        var pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => onHoverEnterCallback(attack, buttonRect));
        eventTrigger.triggers.Add(pointerEnter);

        var pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => onHoverExitCallback());
        eventTrigger.triggers.Add(pointerExit);
    }

    public void OnAttackButtonHoverExit(ref Coroutine hoverCoroutine, ref GameObject currentHoverInstance)
    {
        if (hoverCoroutine != null && monoBehaviourInstance != null)
        {
            monoBehaviourInstance.StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        if (currentHoverInstance != null)
        {
            GameObject.Destroy(currentHoverInstance);
            currentHoverInstance = null;
        }
    }

    public IEnumerator ShowHoverAfterDelay(AttackData attack, RectTransform buttonRect, Action<GameObject> setCurrentHoverInstance)
    {
        yield return new WaitForSeconds(hoverDelay);

        GameObject newHoverInstance = GameObject.Instantiate(attackHoverPrefab, attackPanel.transform);
        SetupHoverText(attack, newHoverInstance);
        PositionHoverUI(buttonRect, newHoverInstance);
        DisableHoverRaycast(newHoverInstance);

        setCurrentHoverInstance?.Invoke(newHoverInstance);
    }

    private void SetupHoverText(AttackData attack, GameObject hoverInstance)
    {
        Text[] hoverTexts = hoverInstance.GetComponentsInChildren<Text>();
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

        if (attack.maxCooldown > 0)
        {
            description += "\nCooldown: " + (attack.cooldown > 0 ? $"{attack.cooldown} turns" : "Ready");
        }

        return description;
    }

    private void PositionHoverUI(RectTransform buttonRect, GameObject hoverInstance)
    {
        RectTransform hoverRect = hoverInstance.GetComponent<RectTransform>();
        Vector2 buttonPos = buttonRect.anchoredPosition;
        hoverRect.anchoredPosition = new Vector2(buttonPos.x, buttonPos.y + buttonRect.sizeDelta.y / 2 + hoverRect.sizeDelta.y / 2 + 10f);
    }

    private void DisableHoverRaycast(GameObject hoverInstance)
    {
        Graphic[] graphics = hoverInstance.GetComponentsInChildren<Graphic>();
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }

    public IEnumerator FlashSprite(SpriteRenderer renderer, AttackData attack)
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

    public void PlayImpactEffect(GameObject target, AttackData attack)
    {
        if (attack == null || target == null)
        {
            Debug.LogWarning("PlayImpactEffect: AttackData or Target GameObject is null.");
            return;
        }

        Animator targetAnimator = target.GetComponent<Animator>();
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("Hit");
        }

        if (GameManager.Instance != null && (attack.soundEffectName == "None" || string.IsNullOrEmpty(attack.soundEffectName)))
        {
            GameManager.Instance.PlaySFX("General/HitSound");
        }

        string effectToLoad = string.IsNullOrEmpty(attack.hitEffectPrefabName) ? attack.effectPrefabName : attack.hitEffectPrefabName;
        GameObject effectPrefab = Resources.Load<GameObject>("Effects/" + effectToLoad);

        if (effectPrefab == null)
        {
            Debug.LogWarning($"Effect prefab not found: {effectToLoad} for attack {attack.attackName}.");
            return;
        }

        Vector3 spawnPosition = target.transform.position;
        Vector3 finalOffset = (attack.attackType == AttackType.Heal) ? attack.effectOffset : (attack.targetHitOffset != Vector3.zero ? attack.targetHitOffset : attack.effectOffset);

        GameObject effectInstance = GameObject.Instantiate(effectPrefab, spawnPosition + finalOffset, Quaternion.identity);

        ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            bool particleSystemDestroysItself = (ps.main.stopAction == ParticleSystemStopAction.Destroy);

            if (!particleSystemDestroysItself)
            {
                float totalEffectDuration = ps.main.duration;
                if (ps.main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                {
                    totalEffectDuration += ps.main.startLifetime.constant;
                }
                else if (ps.main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                {
                    totalEffectDuration += ps.main.startLifetime.constantMax;
                }
                totalEffectDuration += 0.05f;

                if (monoBehaviourInstance != null)
                {
                    monoBehaviourInstance.StartCoroutine(DestroyEffectAfterDelay(effectInstance, totalEffectDuration));
                }
                else
                {
                    GameObject.Destroy(effectInstance, totalEffectDuration);
                }
            }
        }
        else
        {
            if (monoBehaviourInstance != null)
            {
                monoBehaviourInstance.StartCoroutine(DestroyEffectAfterDelay(effectInstance, 2.0f));
            }
            else
            {
                GameObject.Destroy(effectInstance, 2.0f);
            }
        }
    }

    private IEnumerator DestroyEffectAfterDelay(GameObject objToDestroy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (objToDestroy != null)
        {
            GameObject.Destroy(objToDestroy);
        }
    }
}