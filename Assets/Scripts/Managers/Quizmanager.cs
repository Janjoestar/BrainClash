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
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color player1Color = new Color(0.2f, 0.2f, 0.8f); // Blue
    [SerializeField] private Color player2Color = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] internal GameObject Player1;
    [SerializeField] internal GameObject Player2;

    private int currentQuestionIndex = -1;
    private bool canBuzz = true;
    private int playerWhoBuzzed = 0; // 0 = none, 1 = player1, 2 = player2

    private Transform gameTransform;
    private RectTransform quizPanelGameOBJ;
    


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (questions.Count == 0)
            InitializeExampleQuestions();

        // Cache the "Game" object transform
        gameTransform = quizPanel.transform.Find("Game");
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

        Debug.Log(GeneratedQuestionHolder.generatedQuestions[1] + "" + GeneratedQuestionHolder.generatedQuestions[2]);
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
        ShowBuzzerPanel();
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

    private void PlayerBuzzed(int playerNumber)
    {
        canBuzz = false;
        playerWhoBuzzed = playerNumber;
        backgroundImage.color = playerNumber == 1 ? player1Color : player2Color;

        Player1.SetActive(playerNumber == 1);
        Player2.SetActive(playerNumber == 2);

        buzzerPanel.SetActive(false);
        quizPanel.SetActive(true);

        // Move the "Game" object left for player 1 and right for player 2
        if (quizPanelGameOBJ != null)
        {
            float newX = playerNumber == 2 ? -Mathf.Abs(quizPanelGameOBJ.anchoredPosition.x) : Mathf.Abs(quizPanelGameOBJ.anchoredPosition.x);
            quizPanelGameOBJ.anchoredPosition = new Vector2(newX, quizPanelGameOBJ.anchoredPosition.y);
        }

        ShowNextQuestion();
    }

    // Add these variables to your QuizManager class at the top
    private List<int> usedQuestionIndices = new List<int>();

    // Replace ShowNextQuestion method with this version
    private void ShowNextQuestion()
    {
        // Get a question that hasn't been shown yet
        int nextIndex = GetNextUniqueQuestionIndex();
        currentQuestionIndex = nextIndex;

        Question q = questions[currentQuestionIndex];

        questionText.text = q.questionText;
        SetDynamicFontSizeForQuestion(questionText, questionText.text);

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

    // Add this method to get unique question indices
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

        // Make sure we're setting the last correct player
        if (GameManager.Instance != null)
            GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
    }

    private IEnumerator ResetBuzzer()
    {
        questionText.text = "Incorrect! Try again...";
        yield return new WaitForSeconds(1f);

        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);

        backgroundImage.color = Color.white;
        Player1.SetActive(true);
        Player2.SetActive(true);

        canBuzz = true;
        playerWhoBuzzed = 0;
    }

    private void ShowBuzzerPanel()
    {
        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);
        canBuzz = true;
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
