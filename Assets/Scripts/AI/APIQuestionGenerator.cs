using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public static class APIQuestionGenerator
{
    private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private const string API_KEY = "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";
    public static IEnumerator GenerateQuestions(string topic, Action<List<Question>> onComplete, Action<string> onError, int numberOfQuestions, Action<float, string> onProgress = null)
    {
        Debug.Log($"Starting API question generation about {topic}");

        onProgress?.Invoke(0.1f, "Preparing API request...");

        string prompt = CreatePrompt(topic, numberOfQuestions);

        // Clean the prompt to avoid JSON issues
        string cleanPrompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        // Create request data manually
        string jsonRequest = $@"{{
    ""model"": ""llama-3.3-70b-versatile"",
    ""messages"": [
        {{
            ""role"": ""user"",
            ""content"": ""{cleanPrompt}""
        }}
    ],
    ""temperature"": 1.0,
    ""max_tokens"": 4000
}}";

        Debug.Log($"API Request JSON: {jsonRequest}");
        Debug.Log($"API Key present: {!string.IsNullOrEmpty(API_KEY) && API_KEY != "YOUR_GROQ_API_KEY"}");

        onProgress?.Invoke(0.2f, "Sending request to API...");

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {API_KEY}");
            request.timeout = 60;

            onProgress?.Invoke(0.3f, "Waiting for API response...");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onProgress?.Invoke(0.8f, "Processing API response...");

                string response = request.downloadHandler.text;
                Debug.Log($"API Response: {response}");

                List<Question> questions = ProcessAPIResponse(response);

                if (questions != null && questions.Count > 0)
                {
                    // Trim to requested count if we got more
                    if (questions.Count > numberOfQuestions)
                    {
                        questions = questions.Take(numberOfQuestions).ToList();
                    }

                    onProgress?.Invoke(1.0f, $"Generated {questions.Count} questions!");
                    yield return new WaitForSeconds(0.5f);

                    Debug.Log($"Successfully created {questions.Count} questions via API");
                    onComplete?.Invoke(questions);
                }
                else
                {
                    Debug.LogError("Failed to parse API response");
                    onError?.Invoke("Failed to create valid questions from API. Please try again.");
                }
            }
            else
            {
                Debug.LogError($"API request failed: {request.error} (Code: {request.responseCode})");
                Debug.LogError($"Response body: {request.downloadHandler?.text}");

                string errorMessage = "API request failed. ";

                if (request.responseCode == 401)
                    errorMessage += "Invalid API key.";
                else if (request.responseCode == 400)
                    errorMessage += "Bad request format.";
                else if (request.responseCode == 429)
                    errorMessage += "Rate limit exceeded.";
                else
                    errorMessage += "Please check your internet connection.";

                onError?.Invoke(errorMessage);
            }
        }
    }

    private static string CreatePrompt(string topic, int numberOfQuestions)
    {
        return $@"Generate exactly {numberOfQuestions} multiple-choice quiz questions about ""{topic}"".

CRITICAL REQUIREMENTS:
Make the questions moderately challenging, avoiding overly simple or obvious questions.
Output ONLY a valid JSON array - no other text, explanations, or markdown.
Each question must have exactly 4 answer options.
The ""answer"" field must be an integer (0, 1, 2, or 3) representing the index of the correct option.
Never repeat or reuse questions.
Each question must be distinct and original.
Keep answer options concise and clear.
Focus on factual, objective questions only.
Avoid ""Which of the following"" or ""All of the above/None of the above"" formats.
Ensure questions are moderately challenging, avoiding overly simple or obvious questions.

GOOD EXAMPLE FORMAT:
[
  {{
    ""question"": ""What is the capital of France?"",
    ""options"": [""Berlin"", ""Madrid"", ""Paris"", ""Rome""],
    ""answer"": 2
  }},
  {{
    ""question"": ""Which planet is known as the Red Planet?"",
    ""options"": [""Earth"", ""Mars"", ""Jupiter"", ""Venus""],
    ""answer"": 1
  }}
]

BAD EXAMPLES (AVOID THESE STYLES):
Too easy/obvious:
{{
  ""question"": ""What color is a red apple?"",
  ""options"": [""Red"", ""Blue"", ""Green"", ""Yellow""],
  ""answer"": 0
}}
Ambiguous/Subjective:
{{
  ""question"": ""What is the best type of music?"",
  ""options"": [""Rock"", ""Pop"", ""Jazz"", ""Classical""],
  ""answer"": 0
}}
""Which of the following"" format:
{{
  ""question"": ""Which of the following is a programming language?"",
  ""options"": [""HTML"", ""CSS"", ""JavaScript"", ""XML""],
  ""answer"": 2
}}
Not exactly 4 options:
{{
  ""question"": ""How many continents are there?"",
  ""options"": [""5"", ""7""],
  ""answer"": 1
}}
Incorrect answer index:
{{
  ""question"": ""What is 2 + 2?"",
  ""options"": [""3"", ""4"", ""5"", ""6""],
  ""answer"": 4
}}

Generate exactly {numberOfQuestions} questions about ""{topic}"" now, adhering strictly to the GOOD EXAMPLE FORMAT and CRITICAL REQUIREMENTS:";
    }

    private static List<Question> ProcessAPIResponse(string apiResponse)
    {
        try
        {
            // Parse the API response
            var responseObj = JsonUtility.FromJson<GroqResponse>(apiResponse);

            if (responseObj?.choices != null && responseObj.choices.Length > 0)
            {
                string content = responseObj.choices[0].message.content;
                return ParseQuestionsFromContent(content);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing API response: {ex.Message}");
        }

        return null;
    }

    private static List<Question> ParseQuestionsFromContent(string content)
    {
        try
        {
            // Clean the content and extract JSON
            string cleanContent = content.Trim();

            // Remove markdown code blocks if present
            if (cleanContent.StartsWith("```"))
            {
                int start = cleanContent.IndexOf('[');
                int end = cleanContent.LastIndexOf(']');
                if (start >= 0 && end > start)
                {
                    cleanContent = cleanContent.Substring(start, end - start + 1);
                }
            }

            // Parse the JSON array
            string wrappedJson = "{\"questions\":" + cleanContent + "}";
            QuestionListWrapper wrapper = JsonUtility.FromJson<QuestionListWrapper>(wrappedJson);

            if (wrapper?.questions != null && wrapper.questions.Count > 0)
            {
                return ConvertFromAPIQuestions(wrapper.questions);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing questions from content: {ex.Message}");
        }

        return null;
    }

    private static List<Question> ConvertFromAPIQuestions(List<APIQuestion> apiQuestions)
    {
        List<Question> questions = new List<Question>();

        foreach (APIQuestion apiQ in apiQuestions)
        {
            if (apiQ.options != null && apiQ.options.Length >= 4 && !string.IsNullOrEmpty(apiQ.question))
            {
                questions.Add(new Question
                {
                    questionText = apiQ.question.Trim(),
                    answerOptions = apiQ.options.Take(4).Select(o => o?.Trim()).ToArray(),
                    correctAnswerIndex = Mathf.Clamp(apiQ.answer, 0, 3)
                });
            }
        }

        return questions;
    }

    [Serializable]
    private class GroqResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
    }

    [Serializable]
    private class Message
    {
        public string content;
    }

    [Serializable]
    private class APIQuestion
    {
        public string question;
        public string[] options;
        public int answer;
    }

    [Serializable]
    private class QuestionListWrapper
    {
        public List<APIQuestion> questions;
    }
}