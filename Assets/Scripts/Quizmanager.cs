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
    [SerializeField] private GameObject Player1;
    [SerializeField] private GameObject Player2;

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
        ShuffleQuestions();
        ShowBuzzerPanel();
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

    private void ShowNextQuestion()
    {
        currentQuestionIndex = (currentQuestionIndex + 1) % questions.Count;
        Question q = questions[currentQuestionIndex];

        questionText.text = q.questionText;

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < q.answerOptions.Length)
            {
                answerTexts[i].text = q.answerOptions[i];
                answerTexts[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                answerTexts[i].transform.parent.gameObject.SetActive(false);
            }
        }
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
        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.GoToBattleScene();
        else if (TransitionManager.Instance != null)
            TransitionManager.Instance.TransitionToScene("BattleScene");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
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
