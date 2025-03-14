using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    public CharacterDatabase characterDB;

    // Player 1 Variables
    public Text nameTextP1;
    public SpriteRenderer artworkSpriteP1;
    private int selectedOptionP1 = 0;

    // Player 2 Variables
    public Text nameTextP2;
    public SpriteRenderer artworkSpriteP2;
    private int selectedOptionP2 = 0;

    private int selectedOption = 0;

    void Start()
    {
        if (!PlayerPrefs.HasKey("selectedOptionP1"))
        {
            selectedOptionP1 = 0;
        }
        else
        {
            selectedOptionP1 = PlayerPrefs.GetInt("selectedOptionP1");
        }

        if (!PlayerPrefs.HasKey("selectedOptionP2"))
        {
            selectedOptionP2 = 0;
        }
        else
        {
            selectedOptionP2 = PlayerPrefs.GetInt("selectedOptionP2");
        }

        LoadCharacters();
        UpdateCharacter(1, selectedOptionP1);
        UpdateCharacter(2, selectedOptionP2);
    }

    private void UpdateCharacter(int player, int selectedOption)
    {
        Character character = characterDB.GetCharacter(selectedOption);

        if (player == 1)
        {
            artworkSpriteP1.sprite = character.characterSprite;
            nameTextP1.text = character.characterName;
        }
        else if (player == 2)
        {
            artworkSpriteP2.sprite = character.characterSprite;
            nameTextP2.text = character.characterName;
        }
    }

    public void NextCharacter(int player)
    {
        if (player == 1)
        {
            selectedOptionP1++;
            if (selectedOptionP1 >= characterDB.CharacterCount)
            {
                selectedOptionP1 = 0;
            }
            UpdateCharacter(1, selectedOptionP1);
        }
        else if (player == 2)
        {
            selectedOptionP2++;
            if (selectedOptionP2 >= characterDB.CharacterCount)
            {
                selectedOptionP2 = 0;
            }
            UpdateCharacter(2, selectedOptionP2);
        }
        SaveCharacters();
    }

    public void PreviousCharacter(int player)
    {
        if (player == 1)
        {
            selectedOptionP1--;
            if (selectedOptionP1 < 0)
            {
                selectedOptionP1 = characterDB.CharacterCount - 1;
            }
            UpdateCharacter(1, selectedOptionP1);
        }
        else if (player == 2)
        {
            selectedOptionP2--;
            if (selectedOptionP2 < 0)
            {
                selectedOptionP2 = characterDB.CharacterCount - 1;
            }
            UpdateCharacter(2, selectedOptionP2);
        }
        SaveCharacters();
    }

    public void ConfirmSelection()
    {
        SaveCharacters();

        SceneManager.LoadScene("QuizScene");
    }

    public void SaveCharacters() //call before switch scenes
    {
        PlayerPrefs.SetInt("selectedOptionP1", selectedOptionP1);
        PlayerPrefs.SetInt("selectedOptionP2", selectedOptionP2);
        PlayerPrefs.Save();
    }

    private void LoadCharacters()
    {
        selectedOptionP1 = PlayerPrefs.GetInt("selectedOptionP1", 0);
        selectedOptionP2 = PlayerPrefs.GetInt("selectedOptionP2", 0);
    }
}

