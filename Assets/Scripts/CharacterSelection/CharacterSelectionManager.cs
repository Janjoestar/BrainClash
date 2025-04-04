using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;
using TMPro; // For TextMeshPro InputField
using UnityEngine.Networking;
using System.Text;
using UnityEngine.EventSystems;


public class CharacterSelectionManager : MonoBehaviour
{
    public CharacterDatabase characterDB;
    [SerializeField] private InputField promptInput;
    [SerializeField] private GameObject loadingPanel; // Optional loading UI
    [SerializeField] private Button confirmButton; // Optional loading UI
    public Text[] Texts; // Array of all menu button texts
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

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
        // Add any other fields that might be in the response
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

    // Reference to the AttackDataManager
    private AttackDataManager attackDataManager;

    // Lists to store the current attacks for each player
    private List<AttackData> currentAttacksP1;
    private List<AttackData> currentAttacksP2;

    void Start()
    {
        // Get the game mode from the previous screen
        gameMode = StartScreenManager.GetSelectedGameMode();

        Debug.Log(gameMode);

        onTextHover();

        // Set up the UI based on game mode
        SetupPromptField();


        if(gameMode == "AITrivia")
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

        // Update characters with attack data
        UpdateCharacter(1, selectedOptionP1);
        UpdateCharacter(2, selectedOptionP2);

        SetupAttackButtonListeners();
    }

    void onTextHover()
    {
        // Add hover listeners to menu buttons
        if (Texts != null)
        {
            foreach (Text buttonText in Texts)
            {
                Button button = buttonText.GetComponentInParent<Button>();
                if (button != null)
                {
                    // Add EventTrigger component if it doesn't exist
                    EventTrigger trigger = button.GetComponent<EventTrigger>();
                    if (trigger == null)
                        trigger = button.gameObject.AddComponent<EventTrigger>();

                    // Create entry for pointer enter
                    EventTrigger.Entry enterEntry = new EventTrigger.Entry();
                    enterEntry.eventID = EventTriggerType.PointerEnter;
                    enterEntry.callback.AddListener((data) => { OnButtonHover(buttonText, true); });
                    trigger.triggers.Add(enterEntry);

                    // Create entry for pointer exit
                    EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                    exitEntry.eventID = EventTriggerType.PointerExit;
                    exitEntry.callback.AddListener((data) => { OnButtonHover(buttonText, false); });
                    trigger.triggers.Add(exitEntry);
                }
            }
        }
    }

    public void OnButtonHover(Text buttonText, bool isHovering)
    {
        buttonText.color = isHovering ? hoverColor : normalColor;
    }


    void SetupPromptField()
    {
        if (gameMode == "PromptPlay")
        {
            // Show prompt input field with instructions
            promptInput.gameObject.SetActive(true);

            // Add listener for input changes
            promptInput.onValueChanged.AddListener(OnPromptChanged);
        }
        else
        {
            // For other game modes, hide prompt field
            promptInput.gameObject.SetActive(false);

            // Enable confirm since no prompt is needed
            confirmButton.interactable = true;
        }
    }

    void OnPromptChanged(string newText)
    {
        CheckRequirements();
    }

    void CheckRequirements()
    {
        // In Prompt Play mode, require both character selection and prompt
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
        // Make sure we have all the UI elements before proceeding
        if (buttons == null || names == null)
        {
            Debug.LogError("Attack UI elements not assigned in inspector");
            return;
        }


        // Update each attack button
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < 4)
            {
                // This slot has an attack to display
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

        // Determine the correct trigger name based on the attack index
        if (attackIndex < 3)
        {
            // For buttons 0-2, use Attack1, Attack2, Attack3
            attackTrigger = "Attack" + (attackIndex + 1);
        }
        else
        {
            // For button 3 (4th button), use Special
            attackTrigger = "Special";
        }

        if (playerNum == 1)
        {
            characterObject = artworkSpriteP1.gameObject;
            animator = characterObject.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }
        else if (playerNum == 2)
        {
            characterObject = artworkSpriteP2.gameObject;
            animator = characterObject.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
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

    private static int numberOfQuestionsToGenerate = 10;
    // Add this method to your CharacterSelectionManager class
    public void ConfirmSelection()
    {
        SaveCharacters();
        // Handle different game modes
        if (gameMode == "AITrivia")
        {
            List<Question> aiQuestions = AIQuestionsHolder.GetAIQuestions();

            ShuffleQuestions(aiQuestions);

            // Store in holder
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

            if (loadingPanel != null) loadingPanel.SetActive(true);

            StartCoroutine(AIQuestionGenerator.GenerateQuestions(topic, OnQuestionsReady, OnAIError, numberOfQuestionsToGenerate));
        }
    }


    private void ShuffleQuestions(List<Question> questions)
    {
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Question temp = questions[i];
            questions[i] = questions[randomIndex];
            questions[randomIndex] = temp;
        }
    }

    private void OnQuestionsReady(List<Question> questions)
    {
        if (questions == null)
        {
            Debug.LogError("OnQuestionsReady received null questions list");
            if (loadingPanel != null) loadingPanel.SetActive(false);
            return;
        }

        Debug.Log("OnQuestionsReady received " + questions.Count + " questions");

        if (questions.Count == 0)
        {
            Debug.LogWarning("OnQuestionsReady received empty questions list");
            if (loadingPanel != null) loadingPanel.SetActive(false);
            return;
        }

        // Log the first question to verify format
        if (questions.Count > 0)
        {
            Question firstQ = questions[0];
            Debug.Log("First question: " + firstQ.questionText);
            Debug.Log("Options count: " + (firstQ.answerOptions != null ? firstQ.answerOptions.Length : 0));
            Debug.Log("Correct answer index: " + firstQ.correctAnswerIndex);
        }

        GeneratedQuestionHolder.generatedQuestions = questions;
        Debug.Log("Questions stored in GeneratedQuestionHolder, count: " +
                  GeneratedQuestionHolder.generatedQuestions.Count);

        if (loadingPanel != null) loadingPanel.SetActive(false);

        Debug.Log("About to load QuizScene");
        SceneManager.LoadScene("QuizScene");
    }

    private void OnAIError(string error)
    {
        Debug.LogError("AI error: " + error);

        if (error.Contains("JSON") || error.Contains("parse"))
        {
            Debug.Log("Trying with fallback questions...");
            List<Question> fallbackQuestions = CreateBasicQuestions(promptInput.text, 5);
            OnQuestionsReady(fallbackQuestions);
        }
        else
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }
    }

    private List<Question> CreateBasicQuestions(string topic, int count)
    {
        List<Question> questions = new List<Question>();
        for (int i = 0; i < count; i++)
        {
            questions.Add(new Question
            {
                questionText = $"Question {i + 1} about {topic}",
                answerOptions = new[] { "Option A", "Option B", "Option C", "Option D" },
                correctAnswerIndex = 0
            });
        }
        return questions;
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

