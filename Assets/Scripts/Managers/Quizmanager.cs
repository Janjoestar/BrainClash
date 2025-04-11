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
    [SerializeField] internal GameObject Player1;
    [SerializeField] internal GameObject Player2;
    [SerializeField] private float readingTime = 3f;
    [SerializeField] private Text timerText;
    [SerializeField] private Text LogoText;
    [SerializeField] private float earlyBuzzPenaltyTime = 1.5f; // Time penalty for buzzing too early
    [SerializeField] private float colorTransitionSpeed = 2f;
    [SerializeField] private float initialSlideInTime = 0.75f; // How long the initial animation takes

    [Header("Player 1 UI")]
    [SerializeField] private RectTransform leftColorPanel;  // Panel for Player 1 color
    private Coroutine leftPanelAnimation;
    [SerializeField] private Image leftBackgroundImage;
    private Coroutine player1ColorTransition;
    [SerializeField] private Text player1FreezeTimerText;

    [Header("Player 2 UI")]
    [SerializeField] private RectTransform rightColorPanel; // Panel for Player 2 color
    private Coroutine rightPanelAnimation;
    [SerializeField] private Text player2FreezeTimerText;
    [SerializeField] private Image rightBackgroundImage;
    private Coroutine player2ColorTransition;

    private int currentQuestionIndex = -1;
    private bool canBuzz = true;
    private int playerWhoBuzzed = 0; // 0 = none, 1 = player1, 2 = player2
    private float currentReadingTime;

    private Transform gameTransform;
    private RectTransform quizPanelGameOBJ;
    private List<int> usedQuestionIndices = new List<int>();

    [Header("Freeze UI")]
    private bool isReadingTime = true;
    private bool isBuzzLocked = false;
    private bool[] playerFrozen = new bool[3]; // Index 0 unused, 1 = player1, 2 = player2
    private float[] playerFreezeTimeRemaining = new float[3]; // Remaining freeze time for each player
    private Color frozenColor = new Color(0.7f, 0.9f, 1f); // Light blue "frozen" color

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

            Color player1Color = GameManager.Instance.SelectedCharacterP1.characterColor;
            Color player2Color = GameManager.Instance.SelectedCharacterP2.characterColor;

            leftBackgroundImage.color = player1Color;
            rightBackgroundImage.color = player2Color;

            InitializeColorPanels();

            StartCoroutine(InitialColorPanelSlideIn());
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

        HideFreezeTimers();

        StartQuizPhase();
    }

    private void InitializeColorPanels()
    {
        leftColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP1.characterColor;
        rightColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP2.characterColor;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        leftColorPanel.anchoredPosition = new Vector2(-480,0);

        rightColorPanel.anchoredPosition = new Vector2(480, 0);
    }

    private IEnumerator InitialColorPanelSlideIn()
    {
        Vector2 leftPanelTarget = new Vector2(480, 0);
        Vector2 rightPanelTarget = new Vector2(-480, 0);

        Vector2 leftPanelStart = leftColorPanel.anchoredPosition;
        Vector2 rightPanelStart = rightColorPanel.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime < initialSlideInTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / initialSlideInTime;

            float smoothT = Mathf.SmoothStep(0, 1, t);

            leftColorPanel.anchoredPosition = Vector2.Lerp(leftPanelStart, leftPanelTarget, smoothT);
            rightColorPanel.anchoredPosition = Vector2.Lerp(rightPanelStart, rightPanelTarget, smoothT);

            yield return null;
        }

        leftColorPanel.anchoredPosition = leftPanelTarget;
        rightColorPanel.anchoredPosition = rightPanelTarget;
    }

    private IEnumerator AnimateColorPanelsToBuzz(int playerNumber)
    {
        Vector2 leftPanelTarget, rightPanelTarget;

        if (playerNumber == 1)
        {
            leftPanelTarget = new Vector2(Screen.width / 2f, 0);
            rightPanelTarget = new Vector2(Screen.width * 1.5f, 0);
        }
        else
        {
            leftPanelTarget = new Vector2(-Screen.width / 2f, 0);
            rightPanelTarget = new Vector2(0, 0);
        }

        Vector2 leftPanelStart = leftColorPanel.anchoredPosition;
        Vector2 rightPanelStart = rightColorPanel.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime < colorTransitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / colorTransitionSpeed;

            float smoothT = Mathf.SmoothStep(0, 1, t);

            leftColorPanel.anchoredPosition = Vector2.Lerp(leftPanelStart, leftPanelTarget, smoothT);
            rightColorPanel.anchoredPosition = Vector2.Lerp(rightPanelStart, rightPanelTarget, smoothT);

            yield return null;
        }

        leftColorPanel.anchoredPosition = leftPanelTarget;
        rightColorPanel.anchoredPosition = rightPanelTarget;
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
        // Handle buzzer input with proper checks
        HandleBuzzerInput();

        // Update freeze timers
        UpdateFreezeTimers();
    }

    private void UpdateFreezeTimers()
    {
        if (playerFrozen[1])
        {
            playerFreezeTimeRemaining[1] -= Time.deltaTime;
            if (playerFreezeTimeRemaining[1] <= 0)
            {
                playerFrozen[1] = false;
                player1FreezeTimerText.gameObject.SetActive(false);
            }
            else
            {
                player1FreezeTimerText.text = $"Player 1 is frozen for {playerFreezeTimeRemaining[1]:F1} seconds";
            }
        }

        if (playerFrozen[2])
        {
            playerFreezeTimeRemaining[2] -= Time.deltaTime;
            if (playerFreezeTimeRemaining[2] <= 0)
            {
                playerFrozen[2] = false;
                player2FreezeTimerText.gameObject.SetActive(false);
            }
            else
            {
                player2FreezeTimerText.text = $"Player 2 is frozen for {playerFreezeTimeRemaining[2]:F1} seconds";
            }
        }
    }

    private void HideFreezeTimers()
    {
        if (player1FreezeTimerText != null)
            player1FreezeTimerText.gameObject.SetActive(false);

        if (player2FreezeTimerText != null)
            player2FreezeTimerText.gameObject.SetActive(false);
    }

    private void HandleBuzzerInput()
    {
        // If the game isn't in a state where buzzing is allowed, don't process inputs
        if (isBuzzLocked)
            return;

        // Player 1 input
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (isReadingTime)
            {
                // Player buzzed too early - apply penalty
                ApplyEarlyBuzzPenalty(1);
            }
            else if (canBuzz && !playerFrozen[1])
            {
                // Valid buzz
                PlayerBuzzed(1);
            }
        }
        // Player 2 input
        else if (Input.GetKeyDown(KeyCode.K))
        {
            if (isReadingTime)
            {
                // Player buzzed too early - apply penalty
                ApplyEarlyBuzzPenalty(2);
            }
            else if (canBuzz && !playerFrozen[2])
            {
                // Valid buzz
                PlayerBuzzed(2);
            }
        }
    }

    private void ApplyEarlyBuzzPenalty(int playerNumber)
    {
        // Don't apply penalty if player is already frozen
        if (playerFrozen[playerNumber])
            return;

        // Set player as frozen
        playerFrozen[playerNumber] = true;
        playerFreezeTimeRemaining[playerNumber] = earlyBuzzPenaltyTime;

        // Show freeze timer text
        if (playerNumber == 1 && player1FreezeTimerText != null)
        {
            player1FreezeTimerText.gameObject.SetActive(true);
            player1FreezeTimerText.text = $"Frozen for {earlyBuzzPenaltyTime:F1} seconds";
            StartCoroutine(FlashPlayerFreeze(Player1));
        }
        else if (playerNumber == 2 && player2FreezeTimerText != null)
        {
            player2FreezeTimerText.gameObject.SetActive(true);
            player2FreezeTimerText.text = $"Frozen for {earlyBuzzPenaltyTime:F1} seconds";
            StartCoroutine(FlashPlayerFreeze(Player2));
        }
    }

    private IEnumerator FlashPlayerFreeze(GameObject playerObject)
    {
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = frozenColor;
            yield return new WaitForSeconds(0.2f); // Flash briefly
            spriteRenderer.color = originalColor;
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
        // Reset buzz state
        isBuzzLocked = false;
        canBuzz = true;
        playerWhoBuzzed = 0;
        playerFrozen[1] = false;
        playerFrozen[2] = false;

        // Hide freeze timer texts
        HideFreezeTimers();

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
        isReadingTime = true;
        StartCoroutine(QuizPhaseTimer());
    }

    private IEnumerator QuizPhaseTimer()
    {
        Color player1Color = GameManager.Instance.SelectedCharacterP1.characterColor;
        Color player2Color = GameManager.Instance.SelectedCharacterP2.characterColor;

        while (currentReadingTime > 0)
        {
            timerText.text = "Time: " + Mathf.Ceil(currentReadingTime).ToString();

            currentReadingTime -= Time.deltaTime;
            yield return null;
        }

        timerText.text = "Time's up!";

        isReadingTime = false;

        yield return new WaitForSeconds(0.5f);

        MoveToBuzzerPhase();
    }

    private void MoveToBuzzerPhase()
    {
        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);
        answerPanel.SetActive(false);

        Player1.SetActive(true);
        Player2.SetActive(true);

        // Allow buzzing, but keep any active freeze penalties
        canBuzz = true;
        playerWhoBuzzed = 0;
        isBuzzLocked = false;
    }

    public void PlayerBuzzed(int playerNumber)
    {
        if (isBuzzLocked || isReadingTime || !canBuzz || playerFrozen[playerNumber])
            return;

        isBuzzLocked = true;
        canBuzz = false;
        playerWhoBuzzed = playerNumber;

        Color playerColor = playerNumber == 1 ? GameManager.Instance.SelectedCharacterP1.characterColor : GameManager.Instance.SelectedCharacterP2.characterColor;

        if (player1ColorTransition != null)
            StopCoroutine(player1ColorTransition);
        if (player2ColorTransition != null)
            StopCoroutine(player2ColorTransition);

        player1ColorTransition = StartCoroutine(TransitionColor(leftBackgroundImage, playerColor));
        player2ColorTransition = StartCoroutine(TransitionColor(rightBackgroundImage, playerColor));

        questionText.color = playerColor;
        LogoText.color = playerColor;

        Player1.SetActive(playerNumber == 1);
        Player2.SetActive(playerNumber == 2);

        HideFreezeTimers();

        buzzerPanel.SetActive(false);
        quizPanel.SetActive(false);
        answerPanel.SetActive(true);

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

    private IEnumerator TransitionColor(Image image, Color targetColor)
    {
        Color startColor = image.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
    }

    private void ShowAnswerOptions()
    {
        Question q = questions[currentQuestionIndex];

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < q.answerOptions.Length)
            {
                string optionLetter = ((char)('A' + i)).ToString() + ". ";
                answerTexts[i].text = optionLetter + q.answerOptions[i];

                SetDynamicFontSize(answerTexts[i], q.answerOptions[i]);

                answerTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private int GetNextUniqueQuestionIndex()
    {
        if (usedQuestionIndices.Count >= questions.Count)
        {
            usedQuestionIndices.Clear();
        }

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, questions.Count);
        } while (usedQuestionIndices.Contains(randomIndex));

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
        if (content.Length < 10)
        {
            textComponent.fontSize = 50;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        }
        else if (content.Length < 25)
        {
            textComponent.fontSize = 44;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
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
            textComponent.fontSize = 25;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        }

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
        isBuzzLocked = true;

        questionText.text = "Correct! Preparing for battle...";

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
    }

    private IEnumerator ResetBuzzer()
    {
        isBuzzLocked = true;

        questionText.text = "Incorrect! Try again...";

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
        {
            Color player1Color = GameManager.Instance.SelectedCharacterP1.characterColor;
            Color player2Color = GameManager.Instance.SelectedCharacterP2.characterColor;

            if (player1ColorTransition != null)
                StopCoroutine(player1ColorTransition);
            if (player2ColorTransition != null)
                StopCoroutine(player2ColorTransition);

            player1ColorTransition = StartCoroutine(TransitionColor(leftBackgroundImage, player1Color));
            player2ColorTransition = StartCoroutine(TransitionColor(rightBackgroundImage, player2Color));
        }

        questionText.color = Color.white;
        LogoText.color = Color.white;

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