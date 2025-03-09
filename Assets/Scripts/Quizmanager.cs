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

    private int currentQuestionIndex = 0;
    private bool canBuzz = true;
    private int playerWhoBuzzed = 0; // 0 = none, 1 = player1, 2 = player2

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Initialize with example questions if none are set
        if (questions.Count == 0)
        {
            InitializeExampleQuestions();
        }
    }

    private void Start()
    {
        ShowBuzzerPanel();
    }

    private void Update()
    {
        // Check for player buzzing
        if (canBuzz)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                PlayerBuzzed(1);
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                PlayerBuzzed(2);
            }
        }
    }

    private void PlayerBuzzed(int playerNumber)
    {
        canBuzz = false;
        playerWhoBuzzed = playerNumber;
        Debug.Log("Player " + playerNumber + " buzzed in first!");

        // Change background color based on player
        if (backgroundImage != null)
        {
            backgroundImage.color = playerNumber == 1 ? player1Color : player2Color;
        }

        // Show question
        buzzerPanel.SetActive(false);
        quizPanel.SetActive(true);
        ShowQuestion(currentQuestionIndex);
    }

    private void ShowQuestion(int index)
    {
        if (index < questions.Count)
        {
            Question q = questions[index];
            questionText.text = q.questionText;
            Debug.Log("Showing question: " + q.questionText);

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
    }

    public void SelectAnswer(int answerIndex)
    {
        Debug.Log("Selected answer: " + answerIndex + ", Correct answer is: " + questions[currentQuestionIndex].correctAnswerIndex);

        if (answerIndex == questions[currentQuestionIndex].correctAnswerIndex)
        {
            // Correct answer
            Debug.Log("Correct answer! Player " + playerWhoBuzzed + " gets to attack!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetLastCorrectPlayer(playerWhoBuzzed);
            }
            StartCoroutine(TransitionToBattle());
        }
        else
        {
            // Wrong answer
            Debug.Log("Wrong answer! Back to buzzer.");
            StartCoroutine(ResetBuzzer());
        }
    }

    private IEnumerator TransitionToBattle()
    {
        // Visual feedback for correct answer
        questionText.text = "Correct! Preparing for battle...";

        yield return new WaitForSeconds(1f);

        // Advance to next question for when we return
        currentQuestionIndex++;
        if (currentQuestionIndex >= questions.Count)
        {
            currentQuestionIndex = 0; // Loop back to first question
        }

        // Transition to battle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToBattleScene();
        }
        else if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.TransitionToScene("BattleScene");
        }
        else
        {
            Debug.LogError("No GameManager or TransitionManager found!");
            UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
        }
    }

    private IEnumerator ResetBuzzer()
    {
        // Visual feedback for wrong answer
        questionText.text = "Incorrect! Try again...";

        yield return new WaitForSeconds(1f);

        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white; // Reset background
        }
        canBuzz = true;
        playerWhoBuzzed = 0;
    }

    private void ShowBuzzerPanel()
    {
        quizPanel.SetActive(false);
        buzzerPanel.SetActive(true);
        canBuzz = true;
    }

    // Call this to prepare for the next question
    public void NextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex >= questions.Count)
        {
            // End of questions - handle game end
            Debug.Log("End of questions reached!");
            currentQuestionIndex = 0; // Loop back to start
        }

        ShowBuzzerPanel();
    }

    private void InitializeExampleQuestions()
    {
        // Example questions (same as original)
        // Example 1
        Question q1 = new Question
        {
            questionText = "What is the capital of France?",
            answerOptions = new string[] { "London", "Paris", "Berlin", "Madrid" },
            correctAnswerIndex = 1
        };
        questions.Add(q1);

        // Example 2
        Question q2 = new Question
        {
            questionText = "What is the largest planet in our solar system?",
            answerOptions = new string[] { "Earth", "Mars", "Jupiter", "Saturn" },
            correctAnswerIndex = 2
        };
        questions.Add(q2);

        // Example 3
        Question q3 = new Question
        {
            questionText = "How many sides does a pentagon have?",
            answerOptions = new string[] { "4", "5", "6", "7" },
            correctAnswerIndex = 1
        };
        questions.Add(q3);

        // Example 4
        Question q4 = new Question
        {
            questionText = "Which element has the chemical symbol 'O'?",
            answerOptions = new string[] { "Gold", "Oxygen", "Iron", "Osmium" },
            correctAnswerIndex = 1
        };
        questions.Add(q4);

        // Example 5
        Question q5 = new Question
        {
            questionText = "What is 7 × 8?",
            answerOptions = new string[] { "54", "56", "58", "62" },
            correctAnswerIndex = 1
        };
        questions.Add(q5);
    }
}