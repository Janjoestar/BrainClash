using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private Button passQuestionButton; // Assign in inspector
    [SerializeField] private Button[] answerButtons; // Add this field to store references to answer buttons

    [SerializeField] private Canvas DamageMultiAnimation;

    [Header("Player 1 UI")]
    [SerializeField] private RectTransform leftColorPanel;  // Panel for Player 1 color
    private Coroutine leftPanelAnimation;
    [SerializeField] private Image leftBackgroundImage;
    private Coroutine player1ColorTransition;
    [SerializeField] private Text player1FreezeTimerText;
    [SerializeField] private Text player1HealthText;
    [SerializeField] private Text player1DamageMultiText;

    [Header("Player 2 UI")]
    [SerializeField] private RectTransform rightColorPanel; // Panel for Player 2 color
    private Coroutine rightPanelAnimation;
    [SerializeField] private Image rightBackgroundImage;
    private Coroutine player2ColorTransition;
    [SerializeField] private Text player2FreezeTimerText;
    [SerializeField] private Text player2HealthText;
    [SerializeField] private Text player2DamageMultiText;

    private int currentQuestionIndex = -1;
    private bool canBuzz = true;
    private int playerWhoBuzzed = 0; // 0 = none, 1 = player1, 2 = player2
    private float currentReadingTime;

    private Transform gameTransform;
    private RectTransform quizPanelGameOBJ;
    private HashSet<int> usedQuestionSet = new HashSet<int>(); // More efficient for checking if a question has been used

    private bool showTextAnimation = false;
    private bool answerSelected = false; // Add this flag to track if an answer has been selected

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
        }

        // Initialize question tracking
        usedQuestionSet.Clear();

        HideFreezeTimers();

        StartQuizPhase();
    }

    private void InitializeColorPanels()
    {
        leftColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP1.characterColor;
        rightColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP2.characterColor;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        leftColorPanel.anchoredPosition = new Vector2(-480, 0);

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
        HandleBuzzerInput();

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

        // Check for simultaneous early buzzes first
        bool player1Buzzed = Input.GetKeyDown(KeyCode.S);
        bool player2Buzzed = Input.GetKeyDown(KeyCode.K);

        if (isReadingTime)
        {
            // Handle early buzzes
            if (player1Buzzed && player2Buzzed)
            {
                // Both players buzzed simultaneously and too early
                Debug.Log($"[Buzzer] Player 1 and Player 2 buzzed EARLY. isReadingTime: {isReadingTime}, currentReadingTime: {currentReadingTime}");
                ApplyEarlyBuzzPenalty(1);
                ApplyEarlyBuzzPenalty(2);
                return;
            }
            else if (player1Buzzed)
            {
                // Player 1 buzzed too early
                Debug.Log($"[Buzzer] Player 1 buzzed EARLY. isReadingTime: {isReadingTime}, currentReadingTime: {currentReadingTime}");
                ApplyEarlyBuzzPenalty(1);
                return;
            }
            else if (player2Buzzed)
            {
                // Player 2 buzzed too early
                Debug.Log($"[Buzzer] Player 2 buzzed EARLY. isReadingTime: {isReadingTime}, currentReadingTime: {currentReadingTime}");
                ApplyEarlyBuzzPenalty(2);
                return;
            }
        }
        else
        {
            // Handle valid buzzes (not in reading time)
            if (player1Buzzed && player2Buzzed)
            {
                // Both players buzzed simultaneously - determine who gets priority
                HandleSimultaneousBuzzes();
            }
            else if (player1Buzzed && canBuzz && !playerFrozen[1])
            {
                // Valid buzz from Player 1
                PlayerBuzzed(1);
            }
            else if (player2Buzzed && canBuzz && !playerFrozen[2])
            {
                // Valid buzz from Player 2
                PlayerBuzzed(2);
            }
        }
    }

    // Handle simultaneous valid buzzes
    private void HandleSimultaneousBuzzes()
    {
        // Only proceed if both players can buzz
        if (!canBuzz || (playerFrozen[1] && playerFrozen[2]))
            return;

        // If one player is frozen, the other gets priority
        if (playerFrozen[1])
        {
            PlayerBuzzed(2);
            return;
        }
        else if (playerFrozen[2])
        {
            PlayerBuzzed(1);
            return;
        }

        // If both players can buzz, randomly select one
        int randomPlayer = Random.Range(1, 3); // Returns 1 or 2
        PlayerBuzzed(randomPlayer);
    }

    private void ApplyEarlyBuzzPenalty(int playerNumber)
    {
        Debug.Log($"[Buzzer] Applying early buzz penalty to P{playerNumber} for {earlyBuzzPenaltyTime}s. Player currently frozen: {playerFrozen[playerNumber]}");
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
            yield return new WaitForSeconds(0.5f); // Flash briefly
            spriteRenderer.color = originalColor;
        }
    }

    private void UpdatePlayerInfos()
    {
        player1HealthText.text = "Health: " + GameManager.Instance.GetPlayerHealth(2).ToString();
        player2HealthText.text = "Health: " + GameManager.Instance.GetPlayerHealth(1).ToString();
        player1DamageMultiText.text = "Damage Multiplier: " + GameManager.player2DamageMultiplier.ToString() + "x";
        player2DamageMultiText.text = "Damage Multiplier: " + GameManager.player1DamageMultiplier.ToString() + "x";
    }

    private void StartQuizPhase()
    {
        GameManager.Instance.ResetDamageMultipliers();
        UpdatePlayerInfos();

        // Reset buzz state
        isBuzzLocked = false;
        canBuzz = true;
        playerWhoBuzzed = 0;
        playerFrozen[1] = false;
        playerFrozen[2] = false;

        // Reset pass question state
        questionWasPassed = false;
        playerWhoPassedQuestion = 0;

        // Reset answer selection state
        answerSelected = false;

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

        // Add a small delay to ensure all input processing for this frame is complete
        yield return new WaitForSeconds(0.5f);

        // Check if a player has already buzzed (could happen exactly as time runs out)
        if (playerWhoBuzzed == 0)
        {
            MoveToBuzzerPhase();
        }
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
        Debug.Log($"[Buzzer] PlayerBuzzed attempt by P{playerNumber}. isBuzzLocked: {isBuzzLocked}, canBuzz: {canBuzz}, playerFrozen: {playerFrozen[playerNumber]}, isReadingTime: {isReadingTime}, playerWhoBuzzed: {playerWhoBuzzed}");
        if (isBuzzLocked || (isReadingTime && currentReadingTime > 0) || !canBuzz || playerFrozen[playerNumber] || playerWhoBuzzed == 1 || playerWhoBuzzed == 2)
            return;

        isBuzzLocked = true;
        Debug.Log($"[Buzzer] P{playerNumber} successfully buzzed.");
        canBuzz = false;
        playerWhoBuzzed = playerNumber;
        answerSelected = false; // Reset answer selection state when a player buzzes

        GameObject p1damagePopup;
        GameObject p2damagePopup;

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

        if (playerWhoBuzzed == 1 && showTextAnimation)
        {
            p1damagePopup = Instantiate(DamageMultiAnimation, Player1.transform).gameObject;
            StartCoroutine(DestroyAfterDelay(p1damagePopup, 0.75f));
            showTextAnimation = false;
        }
        else if (playerWhoBuzzed == 2 && showTextAnimation)
        {
            p2damagePopup = Instantiate(DamageMultiAnimation, Player2.transform).gameObject;
            StartCoroutine(DestroyAfterDelay(p2damagePopup, 0.75f));
            showTextAnimation = false;
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
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

    private bool questionWasPassed = false;
    private int playerWhoPassedQuestion = 0;

    public void PassQuestionToOtherPlayer()
    {
        Debug.Log($"[Buzzer] PassQuestionToOtherPlayer initiated by P{playerWhoBuzzed}. questionWasPassed: {questionWasPassed}");
        if (playerWhoBuzzed == 0 || questionWasPassed)
            return;

        questionWasPassed = true;
        playerWhoPassedQuestion = playerWhoBuzzed;

        GameManager.Instance.SetDoubleDamageForPlayer(playerWhoPassedQuestion, 1f); // This adds 1x to the OTHER player's received damage multiplier (e.g., if P1 passes, P2's multiplier increases)

        UpdatePlayerInfos();

        int otherPlayer = (playerWhoBuzzed == 1) ? 2 : 1;
        Debug.Log($"[Buzzer] Question passed from P{playerWhoPassedQuestion} to P{otherPlayer}.");

        StartCoroutine(PassQuestionAnimation(otherPlayer));
    }

    private IEnumerator PassQuestionAnimation(int otherPlayer)
    {
        // Store the current question text
        string currentQuestion = questions[currentQuestionIndex].questionText;

        questionText.text = "Question passed to Player " + otherPlayer + "!";

        yield return new WaitForSeconds(0.5f);

        int originalPlayer = playerWhoBuzzed;

        canBuzz = true;
        isBuzzLocked = false;
        playerWhoBuzzed = 0;

        showTextAnimation = true;
        PlayerBuzzed(otherPlayer);

        // Restore the question text
        questionText.text = currentQuestion;
        SetDynamicFontSizeForQuestion(questionText, currentQuestion);
    }

    // Modify ShowAnswerOptions to reset and show the pass button
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

        if (passQuestionButton != null)
        {
            passQuestionButton.interactable = !questionWasPassed;
        }

        // Make sure all answer buttons are interactable (they might have been disabled in a previous round)
        if (answerButtons != null && answerButtons.Length > 0)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i].interactable = true;
                }
            }
        }
    }

    private int GetNextUniqueQuestionIndex()
    {
        // If we've used all questions, reset the tracking
        if (usedQuestionSet.Count >= questions.Count)
        {
            Debug.Log("All questions have been used. Resetting question pool.");
            usedQuestionSet.Clear();
        }

        int randomIndex;
        int attempts = 0;
        int maxAttempts = questions.Count * 2; // Safeguard against infinite loops

        do
        {
            randomIndex = Random.Range(0, questions.Count);
            attempts++;

            // Safety check to prevent infinite loop
            if (attempts > maxAttempts)
            {
                Debug.LogWarning("Failed to find an unused question after multiple attempts. Clearing question history.");
                usedQuestionSet.Clear();
                break;
            }
        } while (usedQuestionSet.Contains(randomIndex));

        // Add to data structure
        usedQuestionSet.Add(randomIndex);

        Debug.Log($"Selected question {randomIndex}. Used questions: {usedQuestionSet.Count}/{questions.Count}");

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
        Debug.Log($"[Buzzer] SelectAnswer: P{playerWhoBuzzed} selected answer {answerIndex}. Correct is {questions[currentQuestionIndex].correctAnswerIndex}. answerSelected: {answerSelected}");
        // Check if an answer has already been selected
        if (answerSelected)
            return;

        // Mark that an answer has been selected
        answerSelected = true;

        // Disable all answer buttons
        if (answerButtons != null && answerButtons.Length > 0)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i].interactable = false;
                }
            }
        }

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
        {
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
    }

    private IEnumerator ResetBuzzer()
    {
        GameManager.Instance.DamagePlayer(playerWhoBuzzed, 15f, false);
        UpdatePlayerInfos();

        if (playerWhoBuzzed == 1)
            questionText.text = "Player " + playerWhoBuzzed + " Took 15 damage";
        else
            questionText.text = "Player " + playerWhoBuzzed + " Took 15 damage";

        isBuzzLocked = true;

        yield return new WaitForSeconds(1.5f);

        // Check if player health is 0 or less
        float playerHealth = GameManager.Instance.GetPlayerHealth(playerWhoBuzzed);
        if (playerHealth <= 0)
        {
            questionText.text = "Player " + playerWhoBuzzed + " has been defeated!";
            yield return new WaitForSeconds(1.5f);

            // Switch to the battle scene and show end screen
            StartCoroutine(TransitionToBattleEndScreen(playerWhoBuzzed));
        }
        else
        {
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
    }

    private IEnumerator TransitionToBattleEndScreen(int defeatedPlayer)
    {
        isBuzzLocked = true;

        yield return new WaitForSeconds(1f);

        // Load battle scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");

        // We need to pass information about which player was defeated
        GameManager.Instance.SetDefeatedPlayerInQuiz(defeatedPlayer);
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