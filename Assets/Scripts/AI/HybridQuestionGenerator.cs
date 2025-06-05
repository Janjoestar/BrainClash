using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class HybridQuestionGenerator
{
    public static IEnumerator GenerateQuestions(string topic, Action<List<Question>> onComplete, Action<string> onError, int numberOfQuestions, Action<float, string> onProgress = null, MonoBehaviour caller = null)
    {
        onProgress?.Invoke(0.05f, "Checking connectivity...");

        bool hasInternet = false;

        // Check internet connectivity
        yield return CheckInternetConnectivity((result) => hasInternet = result);

        if (hasInternet)
        {
            Debug.Log("Internet available - using API question generator");
            bool apiSuccess = false;

            yield return APIQuestionGenerator.GenerateQuestions(topic,
                (questions) => {
                    apiSuccess = true;
                    onComplete?.Invoke(questions);
                },
                (apiError) => {
                    Debug.LogWarning($"API failed: {apiError}, will fallback to local AI");
                },
                numberOfQuestions, onProgress);

            // If API failed, fallback to local AI
            if (!apiSuccess)
            {
                Debug.Log("API failed, falling back to local AI");
                onProgress?.Invoke(0.1f, "API failed, using local AI...");
                yield return AIQuestionGenerator.GenerateQuestions(topic, onComplete, onError, numberOfQuestions, onProgress);
            }
        }
        else
        {
            Debug.Log("No internet - using local AI question generator");
            onProgress?.Invoke(0.1f, "No internet, using local AI...");
            yield return AIQuestionGenerator.GenerateQuestions(topic, onComplete, onError, numberOfQuestions, onProgress);
        }
    }

    private static IEnumerator CheckInternetConnectivity(Action<bool> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://www.google.com"))
        {
            request.timeout = 5; // Quick timeout for connectivity check
            yield return request.SendWebRequest();

            bool hasInternet = request.result == UnityWebRequest.Result.Success;
            callback?.Invoke(hasInternet);
        }
    }
}