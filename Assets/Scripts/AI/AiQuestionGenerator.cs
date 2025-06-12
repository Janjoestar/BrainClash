using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public static class AIQuestionGenerator
{
    private const string MODEL_NAME = "llama3.2:3b";

    public static IEnumerator GenerateQuestions(string topic, Action<List<Question>> onComplete, Action<string> onError, int numberOfQuestions, Action<float, string> onProgress = null)
{
    Debug.Log($"Starting question generation about {topic} using {MODEL_NAME} model");

    onProgress?.Invoke(0.1f, "Preparing request...");

    string promptText = CreatePrompt(topic, numberOfQuestions);

    string jsonRequest = JsonUtility.ToJson(new OllamaRequest
    {
        model = MODEL_NAME,
        prompt = promptText,
        stream = true,

        options = new OllamaOptions
        {
            temperature = 0.3f,      // Lower for more focused output
            top_p = 0.8f,            // More deterministic
        }
    });

    onProgress?.Invoke(0.2f, "Sending request to AI...");

    using (UnityWebRequest request = new UnityWebRequest("http://localhost:11434/api/generate", "POST"))
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.timeout = 300;

        var operation = request.SendWebRequest();

        float startTime = Time.time;
        StringBuilder streamedResponse = new StringBuilder();

        while (!operation.isDone)
        {
            float elapsed = Time.time - startTime;
            float timeProgress = Mathf.Clamp01(elapsed / 60f); // Assume 60s max generation time

            if (request.downloadHandler != null && request.downloadHandler.data != null)
            {
                string currentResponse = request.downloadHandler.text;
                if (currentResponse.Length > streamedResponse.Length)
                {
                    string newContent = currentResponse.Substring(streamedResponse.Length);
                    streamedResponse.Append(newContent);

                    // Update progress more dynamically during streaming (20% to 80%)
                    float currentStreamProgress = Mathf.Clamp01((float)streamedResponse.Length / 10000f); // Estimate based on response length or another metric
                    float overallProgress = Mathf.Lerp(0.2f, 0.8f, currentStreamProgress);
                    string progressText = GetProgressText(streamedResponse.ToString());
                    onProgress?.Invoke(overallProgress, progressText);
                }
            }

            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            // NEW: More granular progress updates for post-response processing
            onProgress?.Invoke(0.80f, "Received AI response, extracting content...");
            string rawResponse = request.downloadHandler.text;
            string fullResponseText = ExtractStreamedResponse(rawResponse);

            onProgress?.Invoke(0.85f, "Parsing questions from AI response...");
            List<Question> questions = ProcessResponse(fullResponseText);

            onProgress?.Invoke(0.90f, "Validating and finalizing questions...");
            if (questions != null && questions.Count > 0)
            {
                // Trim to requested count if we got more
                if (questions.Count > numberOfQuestions)
                {
                    questions = questions.Take(numberOfQuestions).ToList();
                }

                onProgress?.Invoke(1.0f, $"Generated {questions.Count} questions!");
                yield return new WaitForSeconds(0.5f);

                Debug.Log($"Successfully created {questions.Count} questions");
                onComplete?.Invoke(questions);
            }
            else
            {
                Debug.LogError("Failed to create questions");
                onError?.Invoke("Failed to create valid questions. Please try again.");
            }
        }
        else
        {
            Debug.LogError($"Request failed: {request.error} (Code: {request.responseCode})");
            onError?.Invoke($"Request failed: {request.error}. Please check if Ollama is running and the '{MODEL_NAME}' model is installed.");
        }
    }
}

    private static string GetProgressText(string currentResponse)
    {
        if (string.IsNullOrEmpty(currentResponse))
            return "Waiting for AI response...";

        if (currentResponse.Contains("["))
            return "AI is generating questions...";

        if (currentResponse.Contains("question"))
            return "AI is writing questions...";

        if (currentResponse.Length < 50)
            return "AI is thinking...";
        else
            return "AI is generating questions...";
    }

    private static string ExtractStreamedResponse(string rawResponse)
    {
        StringBuilder fullResponse = new StringBuilder();

        try
        {
            string[] lines = rawResponse.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    OllamaStreamResponse response = JsonUtility.FromJson<OllamaStreamResponse>(line);
                    if (!string.IsNullOrEmpty(response.response))
                    {
                        fullResponse.Append(response.response);
                    }

                    if (response.done)
                    {
                        Debug.Log("Streaming completed");
                        break;
                    }
                }
                catch
                {
                    // Fallback regex extraction
                    Match match = Regex.Match(line, "\"response\":\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                    if (match.Success)
                    {
                        string responseText = match.Groups[1].Value;
                        responseText = responseText.Replace("\\\"", "\"")
                                                 .Replace("\\\\", "\\")
                                                 .Replace("\\n", "\n");
                        fullResponse.Append(responseText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error extracting streamed response: {ex.Message}");
            fullResponse.Append(rawResponse);
        }

        return fullResponse.ToString();
    }

    private static string CreatePrompt(string topic, int numberOfQuestions)
    {
        return $@"Generate exactly {numberOfQuestions} multiple-choice quiz questions about ""{topic}"".

CRITICAL REQUIREMENTS:
1. Output ONLY a valid JSON array - no other text, explanations, or markdown
3. Each question must have exactly 4 answer options
4. The answer field must be 0, 1, 2, or 3 (the index of the correct option)

EXACT FORMAT (copy this structure):
[
  {{
    ""question"": ""What is the basic concept of {topic}?"",
    ""options"": [""Correct answer"", ""Wrong answer 1"", ""Wrong answer 2"", ""Wrong answer 3""],
    ""answer"": 0
  }},
  {{
    ""question"": ""Which statement about {topic} is most accurate?"",
    ""options"": [""Wrong answer 1"", ""Correct answer"", ""Wrong answer 2"", ""Wrong answer 3""],
    ""answer"": 1
  }}
]

Generate exactly {numberOfQuestions} questions about ""{topic}"" now:";
    }

    private static List<Question> ProcessResponse(string fullResponseText)
    {
        // Try direct JSON array parsing
        List<Question> questions = TryParseJsonArray(fullResponseText);

        if (questions != null && questions.Count > 0)
        {
            Debug.Log($"JSON parsing successful: {questions.Count} questions");
            return ValidateQuestions(questions);
        }

        // Try extracting JSON from mixed content
        questions = TryExtractJsonFromText(fullResponseText);

        if (questions != null && questions.Count > 0)
        {
            Debug.Log($"JSON extraction successful: {questions.Count} questions");
            return ValidateQuestions(questions);
        }

        Debug.LogError("All parsing methods failed");
        return null;
    }

    private static List<Question> TryParseJsonArray(string text)
    {
        try
        {
            string cleanedText = CleanJsonText(text);
            int start = cleanedText.IndexOf('[');
            int end = cleanedText.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                string jsonArray = cleanedText.Substring(start, end - start + 1);
                string wrappedJson = "{\"questions\":" + jsonArray + "}";
                QuestionListWrapper wrapper = JsonUtility.FromJson<QuestionListWrapper>(wrappedJson);

                if (wrapper?.questions != null && wrapper.questions.Count > 0)
                {
                    return ConvertFromAIQuestions(wrapper.questions);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Direct JSON parsing failed: {ex.Message}");
        }

        return null;
    }

    private static List<Question> TryExtractJsonFromText(string text)
    {
        try
        {
            string[] patterns = {
                @"\[\s*\{.*?\}\s*\]",
                @"```json\s*(\[.*?\])\s*```",
                @"```\s*(\[.*?\])\s*```"
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string jsonCandidate = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    List<Question> questions = TryParseJsonArray(jsonCandidate);
                    if (questions != null && questions.Count > 0)
                    {
                        return questions;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Mixed content extraction failed: {ex.Message}");
        }

        return null;
    }

    private static string CleanJsonText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = text.Replace("\\n", "\n")
                   .Replace("\\\"", "\"")
                   .Replace("\\\\", "\\")
                   .Replace("\\'", "'");

        int firstBracket = text.IndexOf('[');
        int lastBracket = text.LastIndexOf(']');

        if (firstBracket >= 0 && lastBracket > firstBracket)
        {
            text = text.Substring(firstBracket, lastBracket - firstBracket + 1);
        }

        return text;
    }

    private static List<Question> ValidateQuestions(List<Question> questions)
    {
        if (questions == null) return new List<Question>();

        List<Question> validQuestions = new List<Question>();

        foreach (Question q in questions)
        {
            if (IsValidQuestion(q))
            {
                // Clean the question
                q.questionText = q.questionText?.Trim();
                for (int i = 0; i < q.answerOptions.Length; i++)
                {
                    q.answerOptions[i] = q.answerOptions[i]?.Trim();
                }

                validQuestions.Add(q);
            }
        }

        return validQuestions;
    }

    private static bool IsValidQuestion(Question question)
    {
        if (question == null) return false;
        if (string.IsNullOrWhiteSpace(question.questionText)) return false;
        if (question.answerOptions == null || question.answerOptions.Length != 4) return false;
        if (question.correctAnswerIndex < 0 || question.correctAnswerIndex >= 4) return false;

        foreach (string option in question.answerOptions)
        {
            if (string.IsNullOrWhiteSpace(option)) return false;
        }

        return true;
    }

    private static List<Question> ConvertFromAIQuestions(List<AIQuestion> aiQuestions)
    {
        List<Question> questions = new List<Question>();

        foreach (AIQuestion aiQ in aiQuestions)
        {
            if (aiQ.options != null && aiQ.options.Length >= 4)
            {
                questions.Add(new Question
                {
                    questionText = aiQ.question,
                    answerOptions = aiQ.options.Take(4).ToArray(),
                    correctAnswerIndex = Mathf.Clamp(aiQ.answer, 0, 3)
                });
            }
        }

        return questions;
    }

    [Serializable]
    private class OllamaStreamResponse
    {
        public string response;
        public bool done;
    }

    [Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public OllamaOptions options;
    }

    [Serializable]
    private class OllamaOptions
    {
        public float temperature;
        public float top_p;
        public int num_predict;
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