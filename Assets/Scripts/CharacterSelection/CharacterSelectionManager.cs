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

    // Player 1 Variables
    public Text nameTextP1;
    public SpriteRenderer artworkSpriteP1;
    private int selectedOptionP1 = 0;

    // Player 2 Variables
    public Text nameTextP2;
    public SpriteRenderer artworkSpriteP2;
    private int selectedOptionP2 = 0;

    void Start()
    {
        // Get the game mode from the previous screen
        gameMode = StartScreenManager.GetSelectedGameMode();

        onTextHover();

        // Set up the UI based on game mode
        SetupPromptField();

        // Disable confirm button until requirements are met
        confirmButton.interactable = false;

        if(gameMode == "AITrivia")
        {
            RectTransform confirmButtonRect = confirmButton.GetComponent<RectTransform>();
            Vector3 confirmButtonPos = confirmButtonRect.anchoredPosition;
            confirmButtonPos.y = -350f;
            confirmButtonRect.anchoredPosition = confirmButtonPos;
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
        UpdateCharacter(1, selectedOptionP1);
        UpdateCharacter(2, selectedOptionP2);
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
        }
        else if (player == 2)
        {
            artworkSpriteP2.sprite = character.characterSprite;
            nameTextP2.text = character.characterName;
            ApplyCharacterAnimation(artworkSpriteP2.gameObject, character.characterName);
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
    public void ConfirmSelection()
    {
        SaveCharacters();

        string topic = promptInput.text;
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogWarning("Prompt is empty. Please enter a topic.");
            return;
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);

        StartCoroutine(AIQuestionGenerator.GenerateQuestions(topic, OnQuestionsReady, OnAIError, numberOfQuestionsToGenerate));
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

    // In CharacterSelectionManager.cs
    private void OnAIError(string error)
    {
        Debug.LogError("AI error: " + error);

        // If the error mentions JSON, try a different model as fallback
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

    public void SaveCharacters() //call before switch scenes
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

