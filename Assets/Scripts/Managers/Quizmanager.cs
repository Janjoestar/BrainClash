using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [SerializeField] private List<Question> questions = new List<Question>();
    [SerializeField] private Text questionText;
    [SerializeField] private Text[] answerTexts;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject buzzerPanel;
    [SerializeField] private GameObject answerPanel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] internal GameObject Player1;
    [SerializeField] internal GameObject Player2;
    [SerializeField] private float readingTime = 3f;
    [SerializeField] private Text timerText;
    [SerializeField] private Text LogoText;

    private int currentQuestionIndex = -1;
    private bool canBuzz = true;
    private int playerWhoBuzzed = 0; // 0 = none, 1 = player1, 2 = player2
    private float currentReadingTime;

    private Transform gameTransform;
    private RectTransform quizPanelGameOBJ;
    private List<int> usedQuestionIndices = new List<int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (questions.Count == 0)
            InitializeExampleQuestions();

        // Cache the "Game" object transform
        gameTransform = answerPanel.transform.Find("Game");
        if (gameTransform != null)
        {
            quizPanelGameOBJ = gameTransform.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            SetCharacter(Player1, GameManager.Instance.SelectedCharacterP1);
            SetCharacter(Player2, GameManager.Instance.SelectedCharacterP2);
        }

        Debug.Log(GeneratedQuestionHolder.generatedQuestions.Count > 0 ? GeneratedQuestionHolder.generatedQuestions[1] + "" + GeneratedQuestionHolder.generatedQuestions[2] : "No generated questions");

        if (GeneratedQuestionHolder.generatedQuestions != null && GeneratedQuestionHolder.generatedQuestions.Count > 0)
        {
            questions.Clear();
            questions = new List<Question>(GeneratedQuestionHolder.generatedQuestions);
        }
        else
        {
            InitializeExampleQuestions();
            ShuffleQuestions();
        }

        ShuffleQuestions();

        // Start with the quiz panel and display a question
        StartQuizPhase();
    }

    private void SetCharacter(GameObject playerObject, Character character)
    {
        if (character == null) return;

        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = character.characterSprite;
        }

        ApplyCharacterAnimation(playerObject, character.characterName);
    }

    public void ApplyCharacterAnimation(GameObject playerObject, string characterName)
    {
        CharacterAnimation.ApplyCharacterAnimation(playerObject, characterName);
    }

    private void Update()
    {
        if (canBuzz)
        {
            if (Input.GetKeyDown(KeyCode.S))
                PlayerBuzzed(1);
            else if (Input.GetKeyDown(KeyCode.K))
                PlayerBuzzed(2);
        }
    }

    private void ShuffleQuestions()
    {
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (questions[i], questions[randomIndex]) = (questions[randomIndex], questions[i]);
        }
    }

    private void StartQuizPhase()
    {
        // Reset player visibility
        Player1.SetActive(true);
        Player2.SetActive(true);

        // Get a new question
        int nextIndex = GetNextUniqueQuestionIndex();
        currentQuestionIndex = nextIndex;

        // Show quiz panel with question
        quizPanel.SetActive(true);
        buzzerPanel.SetActive(false);
        answerPanel.SetActive(false);

        // Display the question
        Question q = questions[currentQuestionIndex];
        questionText.text = q.questionText;
        SetDynamicFontSizeForQuestion(questionText, questionText.text);

        // Initialize and start the reading timer
        currentReadingTime = readingTime;
        StartCoroutine(QuizPhaseTimer());
    }

    private IEnumerator QuizPhaseTimer()
    {
        Color player1Color = GameManager.Instance.SelectedCharacterP1.characterColor;
        Color player2Color = GameManager.Instance.SelectedCharacterP2.characterColor;

        // Variable to alternate colors between Player1 and Player2
        bool usePlayer1Color = true;

        // Track how much time has passed to switch colors every 1 second
        float colorChangeInterval = 1f; // Change color every 1 second
        float lastColorChangeTime = 0f;

        while (currentReadingTime > 0)
        {
            // Update timer text
            timerText.text = "Time: " + Mathf.Ceil(currentReadingTime).ToString();

            // Switch color at the defined interval
            if (Time.time - lastColorChangeTime >= colorChangeInterval)
            {
                timerText.color = usePlayer1Color ? player1Color : player2Color;
                usePlayer1Color = !usePlayer1Color; // Alternate color
                lastColorChangeTime = Time.time; // Reset the time tracker
            }

            currentReadingTime -= Time.deltaTime;
            yield return null;
        }

        // Timer finished - move to buzzer phase
        timerText.text = "Time's up!";
        timerText.color = Color.white; // Reset the color to default when time's up
        yield return new WaitForSeconds(0.5f);

        // Move to buzzer panel
        MoveToBuzzerPhase();
    }

    private void MoveToBuzzerPhase()
    {
        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);
        answerPanel.SetActive(false);

        // Reset for buzzing
        backgroundImage.color = Color.white;
        Player1.SetActive(true);
        Player2.SetActive(true);

        // Allow buzzing
        canBuzz = true;
        playerWhoBuzzed = 0;
    }

    public void PlayerBuzzed(int playerNumber)
    {
        canBuzz = false;
        playerWhoBuzzed = playerNumber;

        // Use the color of the player who buzzed
        Color playerColor = playerNumber == 1 ? GameManager.Instance.SelectedCharacterP1.characterColor : GameManager.Instance.SelectedCharacterP2.characterColor;

        backgroundImage.color = playerColor;
        questionText.color = playerColor;
        LogoText.color = playerColor;

        Player1.SetActive(playerNumber == 1);
        Player2.SetActive(playerNumber == 2);

        buzzerPanel.SetActive(false);
        quizPanel.SetActive(false);
        answerPanel.SetActive(true);

        // If Player 1 answered, set the answerPanel's x value to 1225
        RectTransform answerPanelRect = answerPanel.GetComponent<RectTransform>();
        if (playerNumber == 1 && answerPanel != null)
        {
            answerPanelRect.localPosition = new Vector2(600f, answerPanelRect.anchoredPosition.y);
        }
        else
        {
            answerPanelRect.localPosition = new Vector2(0f, answerPanelRect.anchoredPosition.y);
        }

        ShowAnswerOptions();
    }

    private void ShowAnswerOptions()
    {
        // Make sure the question is displayed on the answer panel
        // You might need to reference a different text component if you have one specifically for the answer panel
        Question q = questions[currentQuestionIndex];

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < q.answerOptions.Length)
            {
                // Add option letter prefix (A, B, C, D)
                string optionLetter = ((char)('A' + i)).ToString() + ". ";
                answerTexts[i].text = optionLetter + q.answerOptions[i];

                // Dynamically set font size based on text length
                SetDynamicFontSize(answerTexts[i], q.answerOptions[i]);

                answerTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    // Get a unique question index
    private int GetNextUniqueQuestionIndex()
    {
        // If we've used all questions, reset the used list
        if (usedQuestionIndices.Count >= questions.Count)
        {
            usedQuestionIndices.Clear();
        }

        // Find an index we haven't used yet
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, questions.Count);
        } while (usedQuestionIndices.Contains(randomIndex));

        // Mark this index as used
        usedQuestionIndices.Add(randomIndex);

        return randomIndex;
    }

    private void SetDynamicFontSizeForQuestion(Text textComponent, string content)
    {
        if (content.Length < 50)
        {
            textComponent.fontSize = 70;
        }
        else if (content.Length < 75)
        {
            textComponent.fontSize = 60;
        }
        else if (content.Length < 100)
        {
            textComponent.fontSize = 50;
        }
        else
        {
            textComponent.fontSize = 35; // Minimum readable size
        }

        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.alignment = TextAnchor.MiddleCenter;
    }

    private void SetDynamicFontSize(Text textComponent, string content)
    {
        // Scale font size based on content length
        if (content.Length < 10)
        {
            textComponent.fontSize = 50;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else if (content.Length < 25)
        {
            textComponent.fontSize = 44;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        }
        else if (content.Length < 50)
        {
            textComponent.fontSize = 30;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        }
        else
        {
            textComponent.fontSize = 14;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // Ensure alignment is centered
        textComponent.alignment = TextAnchor.MiddleCenter;
    }

    public void SelectAnswer(int answerIndex)
    {
        if (answerIndex == questions[currentQuestionIndex].correctAnswerIndex)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

            StartCoroutine(TransitionToBattle());
        }
        else
        {
            StartCoroutine(ResetBuzzer());
        }
    }

    private IEnumerator TransitionToBattle()
    {
        questionText.text = "Correct! Preparing for battle...";

        // Display the correct message for a moment
        yield return new WaitForSeconds(1f); // Wait for 1 second to show "Correct!"

        // Make sure we're setting the last correct player
        if (GameManager.Instance != null)
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

        // Then transition to the battle scene
        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
    }


    private IEnumerator ResetBuzzer()
    {
        questionText.text = "Incorrect! Try again...";

        // Reset the background color after an incorrect answer
        yield return new WaitForSeconds(1f);

        backgroundImage.color = Color.white;  // Reset to the original color
        questionText.color = Color.white;
        LogoText.color = Color.white;


        // Start a new question phase
        StartQuizPhase();
    }

    private void InitializeExampleQuestions()
    {
        questions.Add(new Question
        {
            questionText = "What is the capital of France?",
            answerOptions = new string[] { "London", "Paris", "Berlin", "Madrid" },
            correctAnswerIndex = 1
        });

        questions.Add(new Question
        {
            questionText = "What is the largest planet in our solar system?",
            answerOptions = new string[] { "Earth", "Mars", "Jupiter", "Saturn" },
            correctAnswerIndex = 2
        });

        questions.Add(new Question
        {
            questionText = "How many sides does a pentagon have?",
            answerOptions = new string[] { "4", "5", "6", "7" },
            correctAnswerIndex = 1
        });

        questions.Add(new Question
        {
            questionText = "Which element has the chemical symbol 'O'?",
            answerOptions = new string[] { "Gold", "Oxygen", "Iron", "Osmium" },
            correctAnswerIndex = 1
        });

        questions.Add(new Question
        {
            questionText = "What is 7 × 8?",
            answerOptions = new string[] { "54", "56", "58", "62" },
            correctAnswerIndex = 1
        });
    }
}