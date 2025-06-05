using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;
    public Text progressText;
    public Text statusText;

    [Header("Settings")]
    public bool smoothProgress = true;
    public float smoothSpeed = 2f;
    public int numberOfQuestionsToGenerate = 20;
    public float errorDisplayTime = 3f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private Coroutine smoothProgressCoroutine;
    private bool hasError = false;

    private void Start()
    {
        InitializeUI();

        string topic = PlayerPrefs.GetString("CurrentTopic", "");
        if (!string.IsNullOrEmpty(topic))
        {
            Debug.Log($"Loading scene started with topic: {topic}");
            StartCoroutine(GenerateQuestionsWithProgress(topic));
        }
        else
        {
            Debug.LogError("No topic found in PlayerPrefs!");
            ShowErrorAndReturnToStart("Error: No topic specified");
        }
    }

    private IEnumerator GenerateQuestionsWithProgress(string topic)
    {
        Debug.Log($"Starting hybrid question generation for topic: {topic}");

        yield return StartCoroutine(HybridQuestionGenerator.GenerateQuestions(
            topic,
            OnQuestionsReady,
            OnAIError,
            numberOfQuestionsToGenerate,
            UpdateProgress
        ));
    }

    private void OnQuestionsReady(List<Question> questions)
    {
        if (hasError) return; // Prevent multiple callbacks

        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("OnQuestionsReady received null or empty questions list");
            ShowErrorAndReturnToStart("No questions were generated");
            return;
        }

        Debug.Log("OnQuestionsReady received " + questions.Count + " questions");

        // Validate questions before storing
        List<Question> validQuestions = ValidateQuestions(questions);

        if (validQuestions.Count == 0)
        {
            Debug.LogError("No valid questions after validation");
            ShowErrorAndReturnToStart("Generated questions were invalid");
            return;
        }

        GeneratedQuestionHolder.generatedQuestions = validQuestions;
        Debug.Log("Questions stored in GeneratedQuestionHolder, count: " + GeneratedQuestionHolder.generatedQuestions.Count);

        CompleteProgress($"Ready! {validQuestions.Count} questions generated.");
        StartCoroutine(LoadQuizSceneAfterDelay(1.5f));
    }

    private List<Question> ValidateQuestions(List<Question> questions)
    {
        List<Question> validQuestions = new List<Question>();

        foreach (Question q in questions)
        {
            if (IsValidQuestion(q))
            {
                validQuestions.Add(q);
            }
            else
            {
                Debug.LogWarning($"Invalid question filtered: {q?.questionText}");
            }
        }

        return validQuestions;
    }

    private bool IsValidQuestion(Question question)
    {
        if (question == null) return false;
        if (string.IsNullOrWhiteSpace(question.questionText)) return false;
        if (question.answerOptions == null || question.answerOptions.Length != 4) return false;
        if (question.correctAnswerIndex < 0 || question.correctAnswerIndex >= 4) return false;

        // Check all options have content
        foreach (string option in question.answerOptions)
        {
            if (string.IsNullOrWhiteSpace(option)) return false;
        }

        return true;
    }

    private IEnumerator LoadQuizSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Loading QuizScene");
        SceneManager.LoadScene("QuizScene");
    }

    private void OnAIError(string error)
    {
        if (hasError) return; // Prevent multiple error callbacks

        hasError = true;
        Debug.LogError("AI error: " + error);
        ShowErrorAndReturnToStart("Failed to generate questions. " + error);
    }

    private void ShowErrorAndReturnToStart(string errorMessage)
    {
        UpdateProgress(0f, errorMessage);
        StartCoroutine(ReturnToStartScreenAfterDelay(errorDisplayTime));
    }

    private IEnumerator ReturnToStartScreenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Returning to StartScreen due to error");
        SceneManager.LoadScene("StartScreen");
    }

    private void InitializeUI()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
        }

        UpdateProgressText(0f);
        UpdateStatusText("Initializing...");
    }

    public void UpdateProgress(float progress, string status = null)
    {
        Debug.Log($"LoadingScreenManager.UpdateProgress called: {progress * 100:F1}% - {status}");

        targetProgress = Mathf.Clamp01(progress);

        if (!string.IsNullOrEmpty(status))
        {
            UpdateStatusText(status);
        }

        if (smoothProgress)
        {
            if (smoothProgressCoroutine == null)
            {
                smoothProgressCoroutine = StartCoroutine(SmoothProgressUpdate());
            }
        }
        else
        {
            SetProgressImmediate(targetProgress);
        }
    }

    private IEnumerator SmoothProgressUpdate()
    {
        while (Mathf.Abs(currentProgress - targetProgress) > 0.01f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, smoothSpeed * Time.deltaTime);
            SetProgressImmediate(currentProgress);
            yield return null;
        }

        SetProgressImmediate(targetProgress);
        smoothProgressCoroutine = null;
    }

    private void SetProgressImmediate(float progress)
    {
        currentProgress = progress;

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        UpdateProgressText(progress);
    }

    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            progressText.text = $"{percentage}%";
        }
    }

    private void UpdateStatusText(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }

        Debug.Log($"Loading Status: {status}");
    }

    public void ResetProgress()
    {
        if (smoothProgressCoroutine != null)
        {
            StopCoroutine(smoothProgressCoroutine);
            smoothProgressCoroutine = null;
        }

        targetProgress = 0f;
        SetProgressImmediate(0f);
        UpdateStatusText("Initializing...");
        hasError = false;
    }

    public void CompleteProgress(string finalMessage = "Complete!")
    {
        if (smoothProgressCoroutine != null)
        {
            StopCoroutine(smoothProgressCoroutine);
            smoothProgressCoroutine = null;
        }

        targetProgress = 1f;
        SetProgressImmediate(1f);
        UpdateStatusText(finalMessage);
    }

    private void OnDestroy()
    {
        if (smoothProgressCoroutine != null)
        {
            StopCoroutine(smoothProgressCoroutine);
        }
    }
}