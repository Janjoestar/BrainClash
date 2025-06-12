using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for EventSystem to check focused UI element

public class CharacterSelectionManager : MonoBehaviour
{
    public CharacterDatabase characterDB;
    [SerializeField] private InputField promptInput;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button confirmButton;

    [System.Serializable]
    public class AIResponse
    {
        public string question;
        public string[] options;
        public int answer;
    }

    [System.Serializable]
    public class OllamaResponseWrapper
    {
        public string response;
        public string model;
        public long created_at;
    }

    string gameMode = StartScreenManager.GetSelectedGameMode();

    [Header("Player 1 UI")]
    public Text nameTextP1;
    public SpriteRenderer artworkSpriteP1;
    private int selectedOptionP1 = 0;
    public Button[] attackButtonsP1;
    public Text[] attackNamesP1;

    [Header("Player 2 UI")]
    public Text nameTextP2;
    public SpriteRenderer artworkSpriteP2;
    private int selectedOptionP2 = 0;
    public Button[] attackButtonsP2;
    public Text[] attackNamesP2;

    private AttackDataManager attackDataManager;

    private List<AttackData> currentAttacksP1;
    private List<AttackData> currentAttacksP2;

    void Start()
    {
        gameMode = StartScreenManager.GetSelectedGameMode();
        Debug.Log(gameMode);

        SetupPromptField();

        if (gameMode == "AITrivia")
        {
            promptInput.interactable = false;
        }

        if (!PlayerPrefs.HasKey("selectedOptionP1"))
        {
            selectedOptionP1 = 0;
        }
        else
        {
            selectedOptionP1 = PlayerPrefs.GetInt("selectedOptionP1");
        }

        if (!PlayerPrefs.HasKey("selectedOptionP2"))
        {
            selectedOptionP2 = 0;
        }
        else
        {
            selectedOptionP2 = PlayerPrefs.GetInt("selectedOptionP2");
        }

        LoadCharacters();

        attackDataManager = AttackDataManager.Instance;

        UpdateCharacter(1, selectedOptionP1);
        UpdateCharacter(2, selectedOptionP2);

        SetupAttackButtonListeners();
    }

    // NEW: Add an Update method to listen for Enter key press
    void Update()
    {
        if (gameMode == "PromptPlay" && promptInput.gameObject.activeSelf)
        {
            // Check if the promptInput is currently the selected (focused) UI element
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == promptInput.gameObject)
            {
                // Check if the Enter key (Return or KeypadEnter) is pressed
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    // Manually deactivate the input field to unfocus it (optional, but good UX)
                    promptInput.DeactivateInputField();
                    ConfirmSelection(); // Call ConfirmSelection only on Enter key press
                }
            }
        }
    }

    void SetupPromptField()
    {
        if (gameMode == "PromptPlay")
        {
            promptInput.gameObject.SetActive(true);
            promptInput.onValueChanged.AddListener(OnPromptChanged);
            // REMOVED: promptInput.onEndEdit.AddListener(OnPromptEndEdit);
            // The OnPromptEndEdit method itself is now also removed as it's no longer needed.
        }
        else
        {
            promptInput.gameObject.SetActive(false);
            confirmButton.interactable = true;
        }
    }

    void OnPromptChanged(string newText)
    {
        CheckRequirements();
    }

    // REMOVED: This method is no longer needed as Enter key handling is now in Update()
    // void OnPromptEndEdit(string text)
    // {
    //     if (!string.IsNullOrEmpty(text) && confirmButton.interactable)
    //     {
    //         ConfirmSelection();
    //     }
    // }

    void CheckRequirements()
    {
        if (gameMode == "PromptPlay")
        {
            confirmButton.interactable = !string.IsNullOrEmpty(promptInput.text);
        }
    }

    private void UpdateCharacter(int player, int selectedOption)
    {
        Character character = characterDB.GetCharacter(selectedOption);

        if (player == 1)
        {
            artworkSpriteP1.sprite = character.characterSprite;
            nameTextP1.text = character.characterName;
            ApplyCharacterAnimation(artworkSpriteP1.gameObject, character.characterName);
            UpdateAttackInfo(1, character.characterName);
        }
        else if (player == 2)
        {
            artworkSpriteP2.sprite = character.characterSprite;
            nameTextP2.text = character.characterName;
            ApplyCharacterAnimation(artworkSpriteP2.gameObject, character.characterName);
            UpdateAttackInfo(2, character.characterName);
        }
    }

    private void UpdateAttackInfo(int player, string characterName)
    {
        List<AttackData> attacks = attackDataManager.GetAttacksForCharacter(characterName);

        if (player == 1)
        {
            currentAttacksP1 = attacks;
            UpdateAttackUI(attacks, attackButtonsP1, attackNamesP1);
        }
        else if (player == 2)
        {
            currentAttacksP2 = attacks;
            UpdateAttackUI(attacks, attackButtonsP2, attackNamesP2);
        }
    }

    private void UpdateAttackUI(List<AttackData> attacks, Button[] buttons, Text[] names)
    {
        if (buttons == null || names == null)
        {
            Debug.LogError("Attack UI elements not assigned in inspector");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < 4)
            {
                buttons[i].gameObject.SetActive(true);

                if (i < names.Length && names[i] != null)
                    names[i].text = attacks[i].attackName;

                ColorBlock colors = buttons[i].colors;
                colors.normalColor = AttackDataManager.Instance.GetColorForAttackType(attacks[i].attackType);
                buttons[i].colors = colors;
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    public void PlayAttackAnimation(int playerNum, int attackIndex)
    {
        GameObject characterObject;
        Animator animator;
        string attackTrigger;
        AttackData attackData = null;

        if (attackIndex < 3)
        {
            attackTrigger = "Attack" + (attackIndex + 1);
        }
        else
        {
            attackTrigger = "Special";
        }

        if (playerNum == 1)
        {
            characterObject = artworkSpriteP1.gameObject;
            animator = characterObject.GetComponent<Animator>();

            if (currentAttacksP1 != null && attackIndex < currentAttacksP1.Count)
                attackData = currentAttacksP1[attackIndex];

            if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }
        else
        {
            characterObject = artworkSpriteP2.gameObject;
            animator = characterObject.GetComponent<Animator>();

            if (currentAttacksP2 != null && attackIndex < currentAttacksP2.Count)
                attackData = currentAttacksP2[attackIndex];

            if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }

        if (attackData != null)
        {
            EffectSpawner effectSpawner = FindObjectOfType<EffectSpawner>();
            if (effectSpawner != null)
            {
                StartCoroutine(effectSpawner.SpawnEffect(characterObject, attackData, true));
            }
        }
    }

    private void SetupAttackButtonListeners()
    {
        for (int i = 0; i < attackButtonsP1.Length; i++)
        {
            int index = i; // Important: Create a local copy for the lambda
            attackButtonsP1[i].onClick.AddListener(() => PlayAttackAnimation(1, index));
        }

        for (int i = 0; i < attackButtonsP2.Length; i++)
        {
            int index = i; // Important: Create a local copy for the lambda
            attackButtonsP2[i].onClick.AddListener(() => PlayAttackAnimation(2, index));
        }
    }

    internal void ApplyCharacterAnimation(GameObject characterPreview, string characterName)
    {
        Animator animator = characterPreview.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component missing on " + characterPreview.name);
            return;
        }

        // Reset Animator to prevent glitches
        animator.runtimeAnimatorController = null;
        animator.Rebind(); // Ensures animation system resets before applying new animation
        animator.Update(0); // Forces Unity to refresh

        // Load Override Controller from Resources
        string overridePath = "Animations/" + characterName + "Override";
        AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);

        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            Debug.LogError("Override Controller not found for " + characterName + " at " + overridePath);
        }
    }

    public void NextCharacter(int player)
    {
        if (player == 1)
        {
            selectedOptionP1++;
            if (selectedOptionP1 >= characterDB.CharacterCount)
            {
                selectedOptionP1 = 0;
            }
            UpdateCharacter(1, selectedOptionP1);
        }
        else if (player == 2)
        {
            selectedOptionP2++;
            if (selectedOptionP2 >= characterDB.CharacterCount)
            {
                selectedOptionP2 = 0;
            }
            UpdateCharacter(2, selectedOptionP2);
        }
        SaveCharacters();
    }

    public void PreviousCharacter(int player)
    {
        if (player == 1)
        {
            selectedOptionP1--;
            if (selectedOptionP1 < 0)
            {
                selectedOptionP1 = characterDB.CharacterCount - 1;
            }
            UpdateCharacter(1, selectedOptionP1);
        }
        else if (player == 2)
        {
            selectedOptionP2--;
            if (selectedOptionP2 < 0)
            {
                selectedOptionP2 = characterDB.CharacterCount - 1;
            }
            UpdateCharacter(2, selectedOptionP2);
        }
        SaveCharacters();
    }

    private static int numberOfQuestionsToGenerate = 5;
    private LoadingScreenManager loadingScreenManager;

    public void ConfirmSelection()
    {
        SaveCharacters();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReloadSelectedCharacters();
        }

        if (gameMode == "AITrivia")
        {
            // Use pre-stored questions from AIQuestionsHolder
            List<Question> aiQuestions = AIQuestionsHolder.GetAIQuestions();
            GeneratedQuestionHolder.generatedQuestions = aiQuestions;
            Debug.Log("AI Trivia questions loaded: " + GeneratedQuestionHolder.generatedQuestions.Count);
            SceneManager.LoadScene("QuizScene");
        }
        else if (gameMode == "PromptPlay")
        {
            string topic = promptInput.text;
            if (string.IsNullOrEmpty(topic))
            {
                Debug.LogWarning("Prompt is empty. Please enter a topic.");
                return;
            }
            Debug.Log($"Starting PromptPlay mode with topic: {topic}");
            // Store the topic for use after scene loads
            PlayerPrefs.SetString("CurrentTopic", topic);
            PlayerPrefs.Save();
            // Load the loading scene first (this will use the hybrid AI generation)
            SceneManager.LoadScene("LoadingScene");
        }
    }

    public void SaveCharacters()
    {
        PlayerPrefs.SetInt("selectedOptionP1", selectedOptionP1);
        PlayerPrefs.SetInt("selectedOptionP2", selectedOptionP2);
        PlayerPrefs.Save();
    }

    private void LoadCharacters()
    {
        selectedOptionP1 = PlayerPrefs.GetInt("selectedOptionP1", 0);
        selectedOptionP2 = PlayerPrefs.GetInt("selectedOptionP2", 0);
    }
}