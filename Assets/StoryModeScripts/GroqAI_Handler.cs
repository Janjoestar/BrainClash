using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;

// C# classes to structure the JSON data for the Groq API request
[Serializable]
public class GroqMessage
{
    public string role;
    public string content;
}

[Serializable]
public class GroqRequest
{
    public GroqMessage[] messages;
    public string model;
}

// C# classes to deserialize the JSON response from the Groq API
[Serializable]
public class GroqChoice
{
    public GroqMessage message;
}

[Serializable]
public class GroqResponse
{
    public GroqChoice[] choices;
}


public class GroqAI_Handler : MonoBehaviour
{
    [Header("Groq API Configuration")]
    [Tooltip("Get your API Key from console.groq.com")]
    [SerializeField] private string apiKey;
    [SerializeField] private string model = "llama3-8b-8192"; // Or another model you prefer

    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    /// <summary>
    /// Coroutine to get a decision from the Groq AI.
    /// </summary>
    /// <param name="prompt">The detailed context and question for the AI.</param>
    /// <param name="callback">The action to call with the AI's response content.</param>
    public IEnumerator GetAiChoice(string prompt, Action<string> callback)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Groq API Key is not set in GroqAI_Handler!");
            callback?.Invoke(null); // Return null to indicate failure
            yield break;
        }

        // Create the request body
        var requestData = new GroqRequest
        {
            model = this.model,
            messages = new GroqMessage[]
            {
                new GroqMessage { role = "system", content = "You are a strategic AI for an enemy in a turn-based RPG. Your goal is to defeat the player. Analyze the provided game state and choose the single best attack from the list of available moves. Your response MUST include a JSON object with your choice, like this: ```json\n{\"attackName\": \"your_chosen_attack_name\"}\n```" },
                new GroqMessage { role = "user", content = prompt }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // Create the web request
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request and wait for the response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseJson = request.downloadHandler.text;
                var groqResponse = JsonUtility.FromJson<GroqResponse>(responseJson);
                string messageContent = groqResponse?.choices[0]?.message?.content;
                callback?.Invoke(messageContent);
            }
            else
            {
                Debug.LogError($"Groq API Error: {request.error}\n{request.downloadHandler.text}");
                callback?.Invoke(null);
            }
        }
    }
}