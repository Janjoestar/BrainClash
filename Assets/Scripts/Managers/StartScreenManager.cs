using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenManager : MonoBehaviour
{
    // Called when the "Play" button is clicked
    public void OnPlayButton()
    {
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
}
