using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

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

    private List<GameObject> enemyUIInstances = new List<GameObject>();

    public UIManager(MonoBehaviour monoBehaviourInstance, GameObject battlePanel, GameObject attackPanel, GameObject enemySelectionPanel, GameObject endScreenPanel, GameObject enemyUIPanel,
                     Text playerHealthText, Text battleStatusText, Text resultText, Button returnToMenuButton, Button continueButton,
                     List<Button> enemySelectionButtons, GameObject attackButtonPrefab, GameObject attackHoverPrefab, GameObject enemyUIElementPrefab,
                     float hoverDelay, Vector3 enemyUIOffset, Text waveReachedText)
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
        this.enemySelectionButtons = enemySelectionButtons;
        this.attackButtonPrefab = attackButtonPrefab;
        this.attackHoverPrefab = attackHoverPrefab;
        this.enemyUIElementPrefab = enemyUIElementPrefab;
        this.hoverDelay = hoverDelay;
        this.enemyUIOffset = enemyUIOffset;
        this.waveReachedText = waveReachedText;
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

    public void CreateEnemyUI(List<GameObject> enemies, List<float> currentEnemyHealths)
    {
        if (enemyUIPanel == null) return;

        enemyUIInstances.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemyUIElementPrefab != null && enemies[i] != null)
            {
                GameObject uiInstance = GameObject.Instantiate(enemyUIElementPrefab, enemyUIPanel.transform);

                RectTransform uiRect = uiInstance.GetComponent<RectTransform>();
                if (uiRect != null)
                {
                    // Reverted to original logic:
                    Vector3 enemyWorldPos = enemies[i].transform.position + enemyUIOffset;
                    uiInstance.transform.position = enemyWorldPos;
                }

                Text[] texts = uiInstance.GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.name.Contains("Name"))
                    {
                        text.text = enemies[i].name.Replace("(Clone)", "").Trim();
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

    public void ClearEnemyUI()
    {
        foreach (GameObject uiInstance in enemyUIInstances)
        {
            if (uiInstance != null)
                GameObject.Destroy(uiInstance);
        }
        enemyUIInstances.Clear();
    }

    public void SetupEnemySelectionButtons(List<int> aliveEnemies, string[] enemyNames, Action<int> onEnemySelectedCallback)
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
                    button.onClick.AddListener(() => onEnemySelectedCallback(capturedIndex));
                }
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

        return description;
    }

    private void PositionHoverUI(RectTransform buttonRect, GameObject hoverInstance)
    {
        RectTransform hoverRect = hoverInstance.GetComponent<RectTransform>();
        Vector2 buttonPos = buttonRect.anchoredPosition;
        hoverRect.anchoredPosition = new Vector2(buttonPos.x, buttonPos.y + 125f);
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
        Animator targetAnimator = target.GetComponent<Animator>();
        if (targetAnimator != null)
            targetAnimator.SetTrigger("Hit");

        if (GameManager.Instance != null)
            GameManager.Instance.PlaySFX("General/HitSound");
    }
}