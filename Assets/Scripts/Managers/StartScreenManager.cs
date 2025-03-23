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
    public GameObject shadowPanel;

    // References for hover effects
    public Text[] menuButtonTexts; // Array of all menu button texts
    public Button[] gameModeButtons; // Array of gamemode buttons

    // Colors for hover effects
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color gameModeNormalColor = Color.white;
    public Color gameModeHoverColor = new Color(1f, 0.8f, 0.8f); // Light pink/red

    // Static variable to store the selected game mode across scenes
    public static string selectedGameMode;

    void Start()
    {
        // Initialize panels as hidden when the scene starts
        if (playPanel != null && shadowPanel != null)
        {
            playPanel.SetActive(false);
            shadowPanel.SetActive(false);
        }

        // Add hover listeners to menu buttons
        if (menuButtonTexts != null)
        {
            foreach (Text buttonText in menuButtonTexts)
            {
                Button button = buttonText.GetComponentInParent<Button>();
                if (button != null)
                {
                    // Add EventTrigger component if it doesn't exist
                    EventTrigger trigger = button.GetComponent<EventTrigger>();
                    if (trigger == null)
                        trigger = button.gameObject.AddComponent<EventTrigger>();

                    // Create entry for pointer enter
                    EventTrigger.Entry enterEntry = new EventTrigger.Entry();
                    enterEntry.eventID = EventTriggerType.PointerEnter;
                    enterEntry.callback.AddListener((data) => { OnButtonHover(buttonText, true); });
                    trigger.triggers.Add(enterEntry);

                    // Create entry for pointer exit
                    EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                    exitEntry.eventID = EventTriggerType.PointerExit;
                    exitEntry.callback.AddListener((data) => { OnButtonHover(buttonText, false); });
                    trigger.triggers.Add(exitEntry);
                }
            }
        }

        // Add hover listeners to gamemode buttons
        if (gameModeButtons != null)
        {
            foreach (Button button in gameModeButtons)
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    // Add EventTrigger component if it doesn't exist
                    EventTrigger trigger = button.GetComponent<EventTrigger>();
                    if (trigger == null)
                        trigger = button.gameObject.AddComponent<EventTrigger>();

                    // Create entry for pointer enter
                    EventTrigger.Entry enterEntry = new EventTrigger.Entry();
                    enterEntry.eventID = EventTriggerType.PointerEnter;
                    enterEntry.callback.AddListener((data) => { OnGameModeHover(buttonImage, true); });
                    trigger.triggers.Add(enterEntry);

                    // Create entry for pointer exit
                    EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                    exitEntry.eventID = EventTriggerType.PointerExit;
                    exitEntry.callback.AddListener((data) => { OnGameModeHover(buttonImage, false); });
                    trigger.triggers.Add(exitEntry);
                }
            }
        }
    }

    // Hover effect for main menu buttons
    public void OnButtonHover(Text buttonText, bool isHovering)
    {
        buttonText.color = isHovering ? hoverColor : normalColor;
    }

    // Hover effect for gamemode buttons
    public void OnGameModeHover(Image buttonImage, bool isHovering)
    {
        buttonImage.color = isHovering ? gameModeHoverColor : gameModeNormalColor;
    }

    // Called when the "Play" button is clicked
    public void OnPlayButton()
    {
        // Show the game mode selection panels
        playPanel.SetActive(true);
        shadowPanel.SetActive(true);
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

    // Called when the "Prep Phase" button is clicked
    public void OnPrepPhaseButton()
    {
        // Add scene name or logic here, for now just logging
        Debug.Log("Prep Phase button clicked");
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
        shadowPanel.SetActive(false);
    }

    // Check if click is outside of gamemode panels
    public void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // If play panel is active, check if click is outside it
            if (playPanel.activeSelf)
            {
                // Cast a ray from the mouse position
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                bool clickedOnGameModeUI = false;

                // Check if any of the hit elements are part of our game mode UI
                foreach (RaycastResult result in results)
                {
                    // Check if the clicked object is a child of our playPanel
                    if (result.gameObject.transform.IsChildOf(playPanel.transform) ||
                        result.gameObject == playPanel)
                    {
                        clickedOnGameModeUI = true;
                        break;
                    }
                }

                // If clicked outside game mode UI, close panels
                if (!clickedOnGameModeUI)
                {
                    playPanel.SetActive(false);
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