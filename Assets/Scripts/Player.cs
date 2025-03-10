using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public CharacterDatabase characterDB;

    public SpriteRenderer artworkSprite;

    private int selectedOption = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDB.GetCharacter(selectedOption);
        artworkSprite.sprite = character.characterSprite;

        // Apply stored scale and position
        artworkSprite.transform.localScale = Vector3.one * character.scaleFactor;
        artworkSprite.transform.localPosition = character.positionOffset;
    }

    private void Load()
    {
        selectedOption = PlayerPrefs.GetInt("selectedOption");
    }
}
