using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public static class AIQuestionGenerator
{
    // Using a more reliable model that's still fast
    private const string MODEL_NAME = "llama3";  // Alternative options: "gemma:2b", "qwen:7b"

    public static IEnumerator GenerateQuestions(string topic, Action<List<Question>> onComplete, Action<string> onError, int numberOfQuestions)
    {
        Debug.Log($"Starting question generation about {topic} using {MODEL_NAME} model");

        // Even clearer prompt with explicit format instructions
        string promptText =
            $"Create {7} multiple choice quiz questions about {topic}. " +
            "Each question should have 4 options with only one correct answer. " +
            "Format as a JSON array following EXACTLY this structure:\n" +
            "[{\"question\":\"Who discovered gravity?\",\"options\":[\"Newton\",\"Einstein\",\"Galileo\",\"Darwin\"],\"answer\":0}]\n" +
            "IMPORTANT: - 'options' must be an array of strings, not objects\n" +
            "- 'answer' must be a number (0-3) indicating the index of the correct option\n" +
            "- Return ONLY the JSON array with no additional text";

        // Create the request body with proper JSON escaping
        string jsonRequest = JsonUtility.ToJson(new OllamaRequest
        {
            model = MODEL_NAME,
            prompt = promptText,
            stream = false
        });

        Debug.Log("Sending request to Ollama with payload: " + jsonRequest);

        using (UnityWebRequest request = new UnityWebRequest("http://localhost:11434/api/generate", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                Debug.Log("Received response, extracting content...");

                // Extract the full response from streaming format
                string fullResponseText = ExtractFullResponse(rawResponse);
                Debug.Log("Extracted text: " + fullResponseText.Substring(0, Mathf.Min(100, fullResponseText.Length)) + "...");

                // Try to extract and process JSON using multiple approaches
                List<Question> questions = null;

                // Approach 1: Try to parse as standard JSON array
                questions = TryParseStandardJsonArray(fullResponseText);

                // Approach 2: If that fails, try fixing malformed JSON
                if (questions == null || questions.Count == 0)
                {
                    Debug.Log("Standard parsing failed, trying to fix JSON format...");
                    questions = TryFixAndParseJson(fullResponseText);
                }

                // Approach 3: If both methods fail, create fallback questions
                if (questions == null || questions.Count == 0)
                {
                    Debug.LogWarning("JSON parsing failed, using fallback questions");
                    questions = CreateFallbackQuestions(topic, numberOfQuestions);
                }

                // We should have questions at this point, either parsed or fallback
                if (questions != null && questions.Count > 0)
                {
                    Debug.Log($"Successfully created {questions.Count} questions");
                    onComplete?.Invoke(questions);
                }
                else
                {
                    Debug.LogError("Failed to create questions through any method");
                    onError?.Invoke("Failed to create valid questions. Please try again with a different topic.");
                }
            }
            else
            {
                // More detailed error logging
                Debug.LogError($"Request failed: {request.error}");
                Debug.LogError($"Response code: {request.responseCode}");
                Debug.LogError($"Response body: {request.downloadHandler?.text}");

                // Check if Ollama is running but with a different model
                onError?.Invoke($"Request failed: {request.error}. Please check if Ollama is running and the '{MODEL_NAME}' model is installed.");
            }
        }
    }

    // Extract the full response text from a potentially streaming response
    private static string ExtractFullResponse(string rawResponse)
    {
        StringBuilder fullResponse = new StringBuilder();

        try
        {
            // Try parsing as a single response first
            OllamaResponse singleResponse = JsonUtility.FromJson<OllamaResponse>(rawResponse);
            if (!string.IsNullOrEmpty(singleResponse.response))
            {
                return singleResponse.response;
            }

            // Split by lines and process each JSON object
            string[] parts = rawResponse.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                try
                {
                    OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(part);
                    if (!string.IsNullOrEmpty(response.response))
                    {
                        fullResponse.Append(response.response);
                    }
                }
                catch (Exception)
                {
                    // Use regex as fallback
                    Match match = Regex.Match(part, "\"response\":\\s*\"(.*?)\"");
                    if (match.Success && match.Groups.Count > 1)
                    {
                        // Unescape the JSON string
                        string responseText = match.Groups[1].Value;
                        responseText = responseText.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n");
                        fullResponse.Append(responseText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error extracting response: {ex.Message}");
        }

        return fullResponse.ToString();
    }

    // Try to parse as a standard JSON array
    private static List<Question> TryParseStandardJsonArray(string text)
    {
        try
        {
            // Find the JSON array in the text
            int start = text.IndexOf('[');
            int end = text.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                string jsonArray = text.Substring(start, end - start + 1);
                Debug.Log("Found JSON array: " + jsonArray.Substring(0, Mathf.Min(100, jsonArray.Length)) + "...");

                // Wrap the array for JsonUtility parsing
                string wrappedJson = "{\"questions\":" + jsonArray + "}";

                QuestionListWrapper wrapper = JsonUtility.FromJson<QuestionListWrapper>(wrappedJson);

                if (wrapper != null && wrapper.questions != null && wrapper.questions.Count > 0)
                {
                    List<Question> questions = new List<Question>();

                    foreach (var q in wrapper.questions)
                    {
                        // Validate the question
                        if (q.options == null || q.options.Length < 2)
                        {
                            continue;
                        }

                        // Ensure answer index is valid
                        int answerIndex = Mathf.Clamp(q.answer, 0, q.options.Length - 1);

                        questions.Add(new Question
                        {
                            questionText = q.question,
                            answerOptions = q.options,
                            correctAnswerIndex = answerIndex
                        });
                    }

                    return questions;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Standard JSON parsing failed: {ex.Message}");
        }

        return null;
    }

    // Try to fix and parse malformed JSON
    private static List<Question> TryFixAndParseJson(string text)
    {
        try
        {
            // Create a list to hold the questions
            List<Question> questions = new List<Question>();

            // Use regex to extract question info
            string pattern = "\"question\":\\s*\"([^\"]+)\"";
            MatchCollection questionMatches = Regex.Matches(text, pattern);

            foreach (Match match in questionMatches)
            {
                if (!match.Success) continue;

                string questionText = match.Groups[1].Value;

                // Look for the options in the text after this question
                int questionPos = text.IndexOf(questionText);
                if (questionPos < 0) continue;

                // Get the text chunk for this question
                int nextQuestionPos = text.IndexOf("\"question\":", questionPos + questionText.Length);
                string questionChunk = nextQuestionPos > 0
                    ? text.Substring(questionPos, nextQuestionPos - questionPos)
                    : text.Substring(questionPos);

                // Extract options
                List<string> options = new List<string>();

                // Try to extract options as an array of strings
                Match optionsMatch = Regex.Match(questionChunk, "\"options\":\\s*\\[(.*?)\\]");
                if (optionsMatch.Success)
                {
                    string optionsText = optionsMatch.Groups[1].Value;
                    MatchCollection optionMatches = Regex.Matches(optionsText, "\"([^\"]+)\"");

                    foreach (Match optMatch in optionMatches)
                    {
                        if (optMatch.Success)
                        {
                            options.Add(optMatch.Groups[1].Value);
                        }
                    }
                }

                // Try to extract options as objects with "text" fields
                if (options.Count == 0)
                {
                    MatchCollection objectOptionMatches = Regex.Matches(questionChunk, "\"text\":\\s*\"([^\"]+)\"");
                    foreach (Match optMatch in objectOptionMatches)
                    {
                        if (optMatch.Success)
                        {
                            options.Add(optMatch.Groups[1].Value);
                        }
                    }
                }

                // If we have at least 2 options, create a question
                if (options.Count >= 2)
                {
                    // Try to find the answer index
                    int answerIndex = 0;
                    Match answerMatch = Regex.Match(questionChunk, "\"answer\":\\s*(\\d+)");
                    if (answerMatch.Success)
                    {
                        int.TryParse(answerMatch.Groups[1].Value, out answerIndex);
                    }

                    // Clamp the answer index to valid range
                    answerIndex = Mathf.Clamp(answerIndex, 0, options.Count - 1);

                    // Create the question
                    questions.Add(new Question
                    {
                        questionText = questionText,
                        answerOptions = options.ToArray(),
                        correctAnswerIndex = answerIndex
                    });
                }
            }

            return questions;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Fix and parse JSON failed: {ex.Message}");
            return null;
        }
    }

    // Create fallback questions if all parsing fails
    private static List<Question> CreateFallbackQuestions(string topic, int count)
    {
        List<Question> fallbackQuestions = new List<Question>();

        // Create simple generic questions about the topic
        for (int i = 0; i < count; i++)
        {
            fallbackQuestions.Add(new Question
            {
                questionText = $"Question {i + 1} about {topic}",
                answerOptions = new string[]
                {
                    "Option A",
                    "Option B",
                    "Option C",
                    "Option D"
                },
                correctAnswerIndex = 0
            });
        }

        Debug.LogWarning("Using fallback questions - these should be replaced with real content");
        return fallbackQuestions;
    }

    [Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    private class OllamaResponse
    {
        public string response;
        public string model;
        public long created_at;
    }

    [Serializable]
    private class AIQuestion
    {
        public string question;
        public string[] options;
        public int answer;
    }

    [Serializable]
    private class QuestionListWrapper
    {
        public List<AIQuestion> questions;
    }
}