using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BuzzerSystem : MonoBehaviour
{
    public Image player1Panel;
    public Image player2Panel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI timerText;

    public GameObject[] answerButtons; // Array of answer buttons
    public List<Question> questions = new List<Question>(); // List of your questions
    private int currentQuestionIndex = 0;

    private bool gameActive = false;
    private int firstPlayer = 0;
    private bool waitingForAnswer = false;
    private float timeRemaining = 13f;
    private bool timerRunning = false;
    private int currentPlayerAnswer = -1; // -1 means no answer yet.

    [System.Serializable] // Make this class visible in the Inspector
    public class Question
    {
        public string question;
        public string[] answers;
        public int correctAnswerIndex;
    }

    void Start()
    {
        ResetGame();
        SetupAnswerButtons();
        LoadQuestions(); // Load questions from your list
    }

    void Update()
    {
        if (!gameActive)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                HandleWinner(1);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                HandleWinner(2);
            }
        }
        else if (waitingForAnswer)
        {
            HandleAnswerInput();
            UpdateTimer();
        }
    }

    private void HandleWinner(int player)
    {
        gameActive = true;
        firstPlayer = player;
        waitingForAnswer = true;

        player1Panel.color = player == 1 ? Color.blue : Color.red;
        player2Panel.color = player == 1 ? Color.red : Color.blue;

        winnerText.text = $"Spieler {player} war zuerst!";
        instructionText.text = player == 1 ? "Drücke 'A' um zu antworten oder 'D' um weiterzugeben." : "Drücke 'J' um zu antworten oder 'L' um weiterzugeben.";
    }

    private void HandleAnswerInput()
    {
        if (firstPlayer == 1)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                winnerText.text = "";
                StartCoroutine(ShowQuestionDelayed(1));
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                winnerText.text = "";
                PassQuestion(1);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                winnerText.text = "";
                StartCoroutine(ShowQuestionDelayed(2));
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                winnerText.text = "";
                PassQuestion(2);
            }
        }
    }

    private IEnumerator ShowQuestionDelayed(int player)
    {
        yield return new WaitForSeconds(1f); // Short delay before showing question.
        ShowQuestion(player);
    }

    private void ShowQuestion(int player)
    {
        waitingForAnswer = false;
        instructionText.text = "Beantworte die Frage!";
        if (currentQuestionIndex < questions.Count)
        {
            questionText.text = questions[currentQuestionIndex].question;
            DisplayAnswers();
            StartTimer();
        }
        else
        {
            questionText.text = "Alle Fragen beantwortet!";
            EndRound();
        }
    }

    private void DisplayAnswers()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < questions[currentQuestionIndex].answers.Length)
            {
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = questions[currentQuestionIndex].answers[i];
                answerButtons[i].SetActive(true);
            }
            else
            {
                answerButtons[i].SetActive(false); // Hide unused buttons
            }
        }
    }

    public void AnswerButtonClicked(int answerIndex)
    {
        if (timerRunning)
        {
            currentPlayerAnswer = answerIndex;
            CheckAnswer();
        }
    }

    private void CheckAnswer()
    {
        if (currentPlayerAnswer == questions[currentQuestionIndex].correctAnswerIndex)
        {
            // Correct answer!
            winnerText.text = "Richtig!";
            // Transition to attack scene here
            StartCoroutine(TransitionToAttackScene());
        }
        else
        {
            // Incorrect answer!
            winnerText.text = "Falsch!";
            EndRound();
        }
    }

    private IEnumerator TransitionToAttackScene()
    {
        yield return new WaitForSeconds(2f); // Short delay before transition
        // Load your attack scene here
        UnityEngine.SceneManagement.SceneManager.LoadScene("AttackScene"); // Replace "AttackScene"
    }

    private void PassQuestion(int player)
    {
        int otherPlayer = (player == 1) ? 2 : 1;

        player1Panel.color = otherPlayer == 1 ? Color.blue : Color.red;
        player2Panel.color = otherPlayer == 1 ? Color.red : Color.blue;

        instructionText.text = $"Spieler {player} hat die Frage weitergegeben! Spieler {otherPlayer} muss antworten.";

        StartCoroutine(ShowQuestionDelayed(otherPlayer));
    }

    private void StartTimer()
    {
        timeRemaining = 13f;
        timerRunning = true;
        timerText.text = "Zeit: 13s";
    }

    private void UpdateTimer()
    {
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = "Zeit: " + Mathf.Ceil(timeRemaining) + "s";

            if (timeRemaining <= 0)
            {
                timerRunning = false;
                EndRound();
            }
        }
    }

    private void EndRound()
    {
        timerRunning = false;
        currentPlayerAnswer = -1;
        currentQuestionIndex++; // Move to the next question
        ResetGame();
    }

    private void ResetGame()
    {
        gameActive = false;
        waitingForAnswer = false;
        firstPlayer = 0;
        timerRunning = false;

        player1Panel.color = Color.blue;
        player2Panel.color = Color.red;

        winnerText.text = "";
        instructionText.text = "Drücke 'S' für Spieler 1 oder 'K' für Spieler 2!";
        questionText.text = "";
        timerText.text = "";

        // Hide answer buttons at start
        foreach (GameObject button in answerButtons)
        {
            button.SetActive(false);
        }
    }

    private void SetupAnswerButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i; // Capture the index for the listener
            answerButtons[i].GetComponent<Button>().onClick.AddListener(() => AnswerButtonClicked(buttonIndex));
        }
    }

    private void LoadQuestions()
    {
        // Add your questions here (replace with your actual questions)
        questions.Add(new Question { question = "Was ist die Hauptstadt von Frankreich?", answers = new string[] { "Berlin", "Paris", "London", "Rom" }, correctAnswerIndex = 1 });
        questions.Add(new Question { question = "Wie viele Planeten hat unser Sonnensystem?", answers = new string[] { "7", "8", "9", "10" }, correctAnswerIndex = 1 });
        // Add more questions...
    }
}