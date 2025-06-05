using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance;

    [Header("Core Components")]
    [SerializeField] private List<Question> questions = new List<Question>();
    [SerializeField] private Text questionText, timerText, LogoText, answerTimerText;
    [SerializeField] private Text[] answerTexts;
    [SerializeField] internal GameObject quizPanel, buzzerPanel, answerPanel, Player1, Player2;
    [SerializeField] private Button passQuestionButton;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private Canvas DamageMultiAnimation;

    [Header("Timing Settings")]
    [SerializeField]
    private float readingTime = 3f, earlyBuzzPenaltyTime = 1.5f, colorTransitionSpeed = 2f,
                                  initialSlideInTime = 0.75f, answerTimeLimit = 10f;

    [Header("Player UI")]
    [SerializeField] private RectTransform leftColorPanel, rightColorPanel;
    [SerializeField] private Image leftBackgroundImage, rightBackgroundImage;
    [SerializeField]
    private Text player1FreezeTimerText, player1HealthText, player1DamageMultiText,
                                  player2FreezeTimerText, player2HealthText, player2DamageMultiText;

    [Header("Audio")]
    [SerializeField] private AudioClip freezeSound, timerTickSound;

    // Consolidate private fields:
    private int currentQuestionIndex = -1, playerWhoBuzzed = 0, playerWhoPassedQuestion = 0;
    private float currentReadingTime, currentAnswerTime;
    private bool canBuzz = true, showTextAnimation = false, answerSelected = false, isReadingTime = true,
                 isBuzzLocked = false, questionWasPassed = false, answerTimerRunning = false;

    private Transform gameTransform;
    private RectTransform quizPanelGameOBJ;
    private AudioSource audioSource;
    private Coroutine player1ColorTransition, player2ColorTransition, answerTimerCoroutine;

    private bool[] playerFrozen = new bool[3];
    private float[] playerFreezeTimeRemaining = new float[3];
    private Color[] originalPlayerColors = new Color[3];
    private Color frozenColor = new Color(0f, 181f / 255f, 240f / 255f, 1f);


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if (questions.Count == 0) Debug.Log("No generated questions found, using default questions.");

        gameTransform = answerPanel.transform.Find("Game");
        if (gameTransform != null) quizPanelGameOBJ = gameTransform.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            SetCharacter(Player1, GameManager.Instance.SelectedCharacterP1);
            SetCharacter(Player2, GameManager.Instance.SelectedCharacterP2);

            leftBackgroundImage.color = GameManager.Instance.SelectedCharacterP1.characterColor;
            rightBackgroundImage.color = GameManager.Instance.SelectedCharacterP2.characterColor;

            StoreOriginalPlayerColors();
            InitializeColorPanels();
            StartCoroutine(InitialColorPanelSlideIn());
        }

        if (GeneratedQuestionHolder.generatedQuestions?.Count > 0)
        {
            questions.Clear();
            questions = new List<Question>(GeneratedQuestionHolder.generatedQuestions);
        }

        HideFreezeTimers();
        StartQuizPhase();
    }

    private void StoreOriginalPlayerColors()
    {
        var p1Renderer = Player1.GetComponent<SpriteRenderer>();
        var p2Renderer = Player2.GetComponent<SpriteRenderer>();

        if (p1Renderer != null) originalPlayerColors[1] = p1Renderer.color;
        if (p2Renderer != null) originalPlayerColors[2] = p2Renderer.color;
    }

    private void InitializeColorPanels()
    {
        leftColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP1.characterColor;
        rightColorPanel.GetComponent<Image>().color = GameManager.Instance.SelectedCharacterP2.characterColor;
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
        UpdatePlayerFreezeTimer(1, player1FreezeTimerText, "Player 1 is frozen for");
        UpdatePlayerFreezeTimer(2, player2FreezeTimerText, "Player 2 is frozen for");
    }

    private void UpdatePlayerFreezeTimer(int playerNum, Text timerText, string message)
    {
        if (!playerFrozen[playerNum]) return;

        playerFreezeTimeRemaining[playerNum] -= Time.deltaTime;
        if (playerFreezeTimeRemaining[playerNum] <= 0)
        {
            playerFrozen[playerNum] = false;
            timerText.gameObject.SetActive(false);
            RestorePlayerColor(playerNum);
        }
        else
        {
            timerText.text = $"{message} {playerFreezeTimeRemaining[playerNum]:F1} seconds";
        }
    }

    private void RestorePlayerColor(int playerNumber)
    {
        GameObject playerObject = (playerNumber == 1) ? Player1 : Player2;
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalPlayerColors[playerNumber];
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
        if (isBuzzLocked && !answerPanel.activeInHierarchy)
            return;

        if (answerPanel.activeInHierarchy && !answerSelected)
        {
            if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectAnswer(0);
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectAnswer(1);
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectAnswer(2);
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectAnswer(3);
                return;
            }
            else if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Space))
            {
                if (passQuestionButton != null && passQuestionButton.interactable)
                {
                    PassQuestionToOtherPlayer();
                }
                return;
            }
        }

        if (answerPanel.activeInHierarchy)
            return;

        bool player1Buzzed = Input.GetKeyDown(KeyCode.S);
        bool player2Buzzed = Input.GetKeyDown(KeyCode.K);

        if (isReadingTime)
        {
            // Handle early buzzes
            if (player1Buzzed && player2Buzzed)
            {
                ApplyEarlyBuzzPenalty(1);
                ApplyEarlyBuzzPenalty(2);
                return;
            }
            else if (player1Buzzed)
            {
                ApplyEarlyBuzzPenalty(1);
                return;
            }
            else if (player2Buzzed)
            {
                ApplyEarlyBuzzPenalty(2);
                return;
            }
        }
        else
        {
            if (player1Buzzed && player2Buzzed)
            {
                HandleSimultaneousBuzzes();
            }
            else if (player1Buzzed && canBuzz && !playerFrozen[1])
            {
                PlayerBuzzed(1);
            }
            else if (player2Buzzed && canBuzz && !playerFrozen[2])
            {
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
        if (playerFrozen[playerNumber]) return;

        playerFrozen[playerNumber] = true;
        playerFreezeTimeRemaining[playerNumber] = earlyBuzzPenaltyTime;
        PlayFreezeSound();

        var timerText = playerNumber == 1 ? player1FreezeTimerText : player2FreezeTimerText;
        var playerObj = playerNumber == 1 ? Player1 : Player2;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = $"Frozen for {earlyBuzzPenaltyTime:F1} seconds";
            ApplyFreezeColor(playerObj);
        }
    }

    private void PlayFreezeSound()
    {
        if (freezeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(freezeSound);
        }
    }

    private void ApplyFreezeColor(GameObject playerObject)
    {
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = frozenColor;
        }
    }

    private void UpdatePlayerInfos()
    {
        player1HealthText.text = $"Health: {GameManager.Instance.GetPlayerHealth(2)}";
        player2HealthText.text = $"Health: {GameManager.Instance.GetPlayerHealth(1)}";
        player1DamageMultiText.text = $"Damage Multiplier: {GameManager.player2DamageMultiplier}x";
        player2DamageMultiText.text = $"Damage Multiplier: {GameManager.player1DamageMultiplier}x";
    }


    private void StartQuizPhase()
    {
        GameManager.Instance.ResetDamageMultipliers();
        UpdatePlayerInfos();

        isBuzzLocked = canBuzz = false;
        canBuzz = true;
        playerWhoBuzzed = playerWhoPassedQuestion = 0;
        playerFrozen[1] = playerFrozen[2] = false;
        questionWasPassed = answerSelected = false;

        RestorePlayerColor(1);
        RestorePlayerColor(2);
        HideFreezeTimers();

        Player1.SetActive(true);
        Player2.SetActive(true);

        currentQuestionIndex = GetNextUniqueQuestionIndex();

        quizPanel.SetActive(true);
        buzzerPanel.SetActive(false);
        answerPanel.SetActive(false);

        var q = questions[currentQuestionIndex];
        questionText.text = q.questionText;
        SetDynamicFontSizeForQuestion(questionText, questionText.text);

        currentReadingTime = readingTime;
        isReadingTime = true;
        answerTimerText.gameObject.SetActive(false);

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

        yield return new WaitForSeconds(0.2f);

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

        canBuzz = true;
        playerWhoBuzzed = 0;
        isBuzzLocked = false;
    }

    public void PlayerBuzzed(int playerNumber)
    {
        if (isBuzzLocked || (isReadingTime && currentReadingTime > 0) || !canBuzz || playerFrozen[playerNumber] || playerWhoBuzzed == 1 || playerWhoBuzzed == 2)
            return;

        isBuzzLocked = true;
        canBuzz = false;
        playerWhoBuzzed = playerNumber;
        answerSelected = false;

        answerTimerText.gameObject.SetActive(true);

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

        StartAnswerTimer();

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

    private void StartAnswerTimer()
    {
        currentAnswerTime = answerTimeLimit;
        answerTimerRunning = true;

        if (answerTimerCoroutine != null)
            StopCoroutine(answerTimerCoroutine);

        answerTimerCoroutine = StartCoroutine(AnswerTimerCountdown());

        // Show the timer text when the timer starts
        answerTimerText.gameObject.SetActive(true);
    }

    private IEnumerator AnswerTimerCountdown()
    {
        float previousTime = currentAnswerTime;
        float lastTickTime = 0f;

        while (currentAnswerTime > 0 && answerTimerRunning && !answerSelected)
        {
            if (answerTimerText != null)
            {
                answerTimerText.text = "Time: " + Mathf.Ceil(currentAnswerTime).ToString();

                float timeFraction = currentAnswerTime / answerTimeLimit;

                if (timeFraction > 0.66f)
                {
                    answerTimerText.color = Color.yellow;
                }
                else if (timeFraction > 0.33f)
                {
                    answerTimerText.color = new Color(1f, 0.647f, 0f);
                }
                else
                {
                    answerTimerText.color = Color.red;
                }
            }

            if (Mathf.Floor(currentAnswerTime) != Mathf.Floor(previousTime))
            {
                previousTime = currentAnswerTime;

                if (timerTickSound != null && audioSource != null)
                {
                    float volume = Mathf.Lerp(0.4f, 1f, currentAnswerTime / answerTimeLimit);
                    audioSource.PlayOneShot(timerTickSound, volume);
                }
            }

            currentAnswerTime -= Time.deltaTime;
            yield return null;
        }

        if (!answerSelected && answerTimerRunning)
        {
            AnswerTimeExpired();
        }
        answerTimerText.gameObject.SetActive(false);
    }


    private void AnswerTimeExpired()
    {
        if (answerSelected) return;

        answerSelected = true;
        answerTimerRunning = false;

        if (answerTimerText != null)
            answerTimerText.text = "Time's Up!";

        answerButtons[questions[currentQuestionIndex].correctAnswerIndex].GetComponent<Image>().color = Color.green;

        StartCoroutine(ResetBuzzer());
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

    public void PassQuestionToOtherPlayer()
    {
        if (playerWhoBuzzed == 0 || questionWasPassed)
            return;

        questionWasPassed = true;
        playerWhoPassedQuestion = playerWhoBuzzed;

        GameManager.Instance.SetDoubleDamageForPlayer(playerWhoPassedQuestion, 1f);

        UpdatePlayerInfos();

        int otherPlayer = (playerWhoBuzzed == 1) ? 2 : 1;

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

    public void ResetAnswerButtons()
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
            {
                button.GetComponent<Image>().color = Color.white; // Reset to default color
                button.transform.localScale = Vector3.one; // Reset to original size
            }
        }
    }
    private void ShowAnswerOptions()
    {
        var q = questions[currentQuestionIndex];

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < q.answerOptions.Length)
            {
                answerTexts[i].text = $"{(char)('A' + i)}. {q.answerOptions[i]}";
                SetDynamicFontSize(answerTexts[i], q.answerOptions[i]);
                answerTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }

        if (passQuestionButton != null) passQuestionButton.interactable = !questionWasPassed;

        foreach (var button in answerButtons)
            if (button != null) button.interactable = true;

        ResetAnswerButtons();
    }

    private int GetNextUniqueQuestionIndex()
    {
        // If we've used all questions, reset the tracking
        if (GameManager.Instance.GetUsedQuestionCount() >= questions.Count)
        {
            Debug.Log("All questions have been used. Resetting question pool.");
            GameManager.Instance.ResetUsedQuestions();
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
                GameManager.Instance.ResetUsedQuestions();
                break;
            }
        } while (GameManager.Instance.IsQuestionUsed(randomIndex));

        // Mark question as used
        GameManager.Instance.MarkQuestionAsUsed(randomIndex);

        Debug.Log($"Selected question {randomIndex}. Used questions: {GameManager.Instance.GetUsedQuestionCount()}/{questions.Count}");

        return randomIndex;
    }

    private void SetDynamicFontSizeForQuestion(Text textComponent, string content)
    {
        int len = content.Length;
        textComponent.fontSize = len < 50 ? 70 : len < 75 ? 60 : len < 100 ? 50 : 35;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.alignment = TextAnchor.MiddleCenter;
    }

    // Simplified SetDynamicFontSize:
    private void SetDynamicFontSize(Text textComponent, string content)
    {
        int len = content.Length;
        textComponent.fontSize = len < 10 ? 50 : len < 25 ? 44 : len < 50 ? 30 : 25;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.alignment = TextAnchor.MiddleCenter;
    }

    public void SelectAnswer(int answerIndex)
    {
        if (answerSelected) return;
        answerSelected = true;
        answerTimerRunning = false; // Stop the answer timer

        if (answerTimerCoroutine != null)
            StopCoroutine(answerTimerCoroutine);

        bool isCorrect = answerIndex == questions[currentQuestionIndex].correctAnswerIndex;
        answerButtons[answerIndex].GetComponent<Image>().color = isCorrect ? Color.green : Color.red;

        if (!isCorrect)
            answerButtons[questions[currentQuestionIndex].correctAnswerIndex].GetComponent<Image>().color = Color.green;

        StartCoroutine(AnimateButtonSize(answerButtons[answerIndex], isCorrect));

        if (isCorrect)
        {
            GameManager.Instance?.SetLastCorrectPlayer(playerWhoBuzzed);
            StartCoroutine(TransitionToBattle());
        }
        else
            StartCoroutine(ResetBuzzer());
    }

    private IEnumerator AnimateButtonSize(Button button, bool isCorrect)
    {
        Vector3 orig = button.transform.localScale;
        Vector3 big = orig * 1.1f;
        float t = 0f;

        while (t < 0.2f)
        {
            button.transform.localScale = Vector3.Lerp(orig, big, t / 0.2f);
            t += Time.deltaTime;
            yield return null;
        }

        button.transform.localScale = big;
        yield return new WaitForSeconds(0.1f);

        t = 0f;
        while (t < 0.2f)
        {
            button.transform.localScale = Vector3.Lerp(big, orig, t / 0.2f);
            t += Time.deltaTime;
            yield return null;
        }

        button.transform.localScale = orig;
    }

    private IEnumerator TransitionToBattle()
    {
        isBuzzLocked = true;

        questionText.text = "Correct! Preparing for battle...";

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

            // If this player passed the question, they'll take double damage
            if (questionWasPassed && playerWhoPassedQuestion == playerWhoBuzzed)
            {
                GameManager.Instance.SetDoubleDamageForPlayer(playerWhoBuzzed, 2f);
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
    }

    private IEnumerator ResetBuzzer()
    {
        GameManager.Instance.DamagePlayer(playerWhoBuzzed, 15f, false);

        GameManager.Instance.PlaySFX("General/HitSound", 1.0f, 0.7f);
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
}