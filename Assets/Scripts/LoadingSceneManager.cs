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

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private Coroutine smoothProgressCoroutine;

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
            UpdateProgress(0f, "Error: No topic specified");
        }
    }

    private IEnumerator GenerateQuestionsWithProgress(string topic)
    {
        Debug.Log($"Starting question generation for topic: {topic}");

        yield return StartCoroutine(AIQuestionGenerator.GenerateQuestions(
            topic,
            OnQuestionsReady,
            OnAIError,
            numberOfQuestionsToGenerate,
            UpdateProgress
        ));
    }

    private void OnQuestionsReady(List<Question> questions)
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("OnQuestionsReady received null or empty questions list");
            OnAIError("No questions were generated");
            return;
        }

        Debug.Log("OnQuestionsReady received " + questions.Count + " questions");

        GeneratedQuestionHolder.generatedQuestions = questions;
        Debug.Log("Questions stored in GeneratedQuestionHolder, count: " + GeneratedQuestionHolder.generatedQuestions.Count);

        CompleteProgress("Questions ready!");
        StartCoroutine(LoadQuizSceneAfterDelay(1f));
    }

    private IEnumerator LoadQuizSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Loading QuizScene");
        SceneManager.LoadScene("QuizScene");
    }

    private void OnAIError(string error)
    {
        Debug.LogError("AI error: " + error);
        UpdateProgress(0f, "Failed to generate questions. Please try again.");
        StartCoroutine(ReturnToPreviousSceneAfterDelay(3f));
    }

    private IEnumerator ReturnToPreviousSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Load back to your main menu or character selection scene
        // SceneManager.LoadScene("YourMainMenuScene");
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