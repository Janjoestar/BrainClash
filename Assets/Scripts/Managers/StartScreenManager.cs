using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartScreenManager : MonoBehaviour
{
    // Reference to the panels
    public GameObject playPanel;
    public GameObject creditsPanel;
    public GameObject shadowPanel;

    // Static variable to store the selected game mode across scenes
    public static string selectedGameMode;

    void Start()
    {
        // Initialize panels as hidden when the scene starts
        if (playPanel != null && creditsPanel != null && shadowPanel != null)
        {
            playPanel.SetActive(false);
            creditsPanel.SetActive(false);
            shadowPanel.SetActive(false);
        }
    }

    // Called when the "Play" button is clicked
    public void OnPlayButton()
    {
        // Show the game mode selection panels
        playPanel.SetActive(true);
        shadowPanel.SetActive(true);
    }

    // Called when the "Credits" button is clicked
    public void OnCreditsButton()
    {
        // Show the credits panel
        creditsPanel.SetActive(true);
        shadowPanel.SetActive(true);
    }

    public void OnPrepPhaseButton()
    {
        SceneManager.LoadScene("PrepPhase");
    }

    // Called when the "Prompt Play" game mode is selected
    public void OnPromptPlayButton()
    {
        // Store the selected game mode
        selectedGameMode = "PromptPlay";
        // Load the character selection scene
        SceneManager.LoadScene("CharacterSelection");
    }

    // Called when the "AI Trivia" game mode is selected
    public void OnAITriviaButton()
    {
        // Store the selected game mode
        selectedGameMode = "AITrivia";
        Debug.Log(selectedGameMode);
        // Load the character selection scene
        SceneManager.LoadScene("CharacterSelection");
    }

    public void OnStoryModeButton()
    {
        // Store the selected game mode
        selectedGameMode = "StoryMode";
        Debug.Log(selectedGameMode);
        // Load the character selection scene
        SceneManager.LoadScene("StoryCharacterSelection");
    }

    // Called when the "Exit" button is clicked
    public void OnExitButton()
    {
        Application.Quit();
        Debug.Log("Exit button clicked. Quitting application.");
    }

    // Called when clicking the shadow panel (background)
    public void OnShadowPanelClick()
    {
        // Hide the panels
        playPanel.SetActive(false);
        creditsPanel.SetActive(false);
        shadowPanel.SetActive(false);
    }

    // Check if click is outside of panels
    public void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // If any panel is active, check if click is outside it
            if (playPanel.activeSelf || creditsPanel.activeSelf)
            {
                // Cast a ray from the mouse position
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                bool clickedOnUI = false;

                // Check if any of the hit elements are part of our UI panels
                foreach (RaycastResult result in results)
                {
                    // Check if the clicked object is a child of our panels
                    if ((playPanel.activeSelf && (result.gameObject.transform.IsChildOf(playPanel.transform) || result.gameObject == playPanel)) ||
                        (creditsPanel.activeSelf && (result.gameObject.transform.IsChildOf(creditsPanel.transform) || result.gameObject == creditsPanel)))
                    {
                        clickedOnUI = true;
                        break;
                    }
                }

                // If clicked outside UI, close panels
                if (!clickedOnUI)
                {
                    playPanel.SetActive(false);
                    creditsPanel.SetActive(false);
                    shadowPanel.SetActive(false);
                }
            }
        }
    }

    // This method can be called from the CharacterSelection scene to get the selected mode
    public static string GetSelectedGameMode()
    {
        return selectedGameMode;
    }
}