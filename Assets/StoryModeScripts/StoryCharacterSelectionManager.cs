// StoryCharacterSelectionManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryCharacterSelectionManager : MonoBehaviour
{
    public CharacterDatabase characterDB;
    [SerializeField] private Button confirmButton;

    [Header("Debug Settings")]
    [SerializeField] private bool unlockAllAbilitiesDebug = false; // <-- DEBUG TOGGLE ADDED

    [Header("Player UI")]
    public Text nameText;
    public SpriteRenderer artworkSprite;
    private int selectedOption = 0;
    public Button[] attackButtons;
    public Text[] attackNames;

    // Change to use StoryAttackDataManager
    private StoryAttackDataManager storyAttackDataManager;
    private List<AttackData> currentAttacks;

    void Start()
    {
        if (!PlayerPrefs.HasKey("selectedStoryCharacter"))
        {
            selectedOption = 0;
        }
        else
        {
            selectedOption = PlayerPrefs.GetInt("selectedStoryCharacter");
        }

        // Initialize the new StoryAttackDataManager
        storyAttackDataManager = StoryAttackDataManager.Instance;
        LoadCharacter();
        UpdateCharacter(selectedOption);
        SetupAttackButtonListeners();
    }

    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDB.GetCharacter(selectedOption);

        artworkSprite.sprite = character.characterSprite;
        nameText.text = character.characterName;
        ApplyCharacterAnimation(artworkSprite.gameObject, character.characterName);
        UpdateAttackInfo(character.characterName);
    }

    private void UpdateAttackInfo(string characterName)
    {
        // If debug toggle is on, get all attacks. Otherwise, get only the starting one.
        if (unlockAllAbilitiesDebug)
        {
            currentAttacks = storyAttackDataManager.GetAttacksForCharacter(characterName);
        }
        else
        {
            // Original behavior
            List<AttackData> attacks = new List<AttackData>();
            AttackData startingAttack = storyAttackDataManager.GetStartingAttackForCharacter(characterName);
            if (startingAttack != null)
            {
                attacks.Add(startingAttack);
            }
            currentAttacks = attacks;
        }
        UpdateAttackUI(currentAttacks, attackButtons, attackNames);
    }

    private void UpdateAttackUI(List<AttackData> attacks, Button[] buttons, Text[] names)
    {
        if (buttons == null || names == null)
        {
            Debug.LogError("Attack UI elements not assigned in inspector");
            return;
        }

        // Loop through all available button slots
        for (int i = 0; i < buttons.Length; i++)
        {
            // If there's an attack for this button slot, activate and configure it
            if (i < attacks.Count)
            {
                buttons[i].gameObject.SetActive(true);

                if (i < names.Length && names[i] != null)
                    names[i].text = attacks[i].attackName;

                ColorBlock colors = buttons[i].colors;
                colors.normalColor = storyAttackDataManager.GetColorForAttackType(attacks[i].attackType);
                buttons[i].colors = colors;
            }
            else
            {
                // If there's no attack for this slot, deactivate the button
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    public void PlayAttackAnimation(int attackIndex)
    {
        // Check if the provided index is valid for the current attacks list
        if (currentAttacks == null || attackIndex < 0 || attackIndex >= currentAttacks.Count)
        {
            Debug.LogError("Invalid attack index for animation preview.");
            return;
        }

        GameObject characterObject = artworkSprite.gameObject;
        Animator animator = characterObject.GetComponent<Animator>();

        // Get the specific attack data using the button's index
        AttackData attackData = currentAttacks[attackIndex];

        // Use the animation trigger defined in the AttackData
        string attackTrigger = attackData.animationTrigger;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        if (attackData != null)
        {
            EffectSpawner effectSpawner = FindObjectOfType<EffectSpawner>();
            if (effectSpawner != null)
            {
                StartCoroutine(effectSpawner.SpawnEffect(characterObject, attackData, true));
            }
        }
    }

    private void SetupAttackButtonListeners()
    {
        for (int i = 0; i < attackButtons.Length; i++)
        {
            int index = i;
            attackButtons[i].onClick.AddListener(() => PlayAttackAnimation(index));
        }
    }

    internal void ApplyCharacterAnimation(GameObject characterPreview, string characterName)
    {
        Animator animator = characterPreview.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component missing on " + characterPreview.name);
            return;
        }

        animator.runtimeAnimatorController = null;
        animator.Rebind();
        animator.Update(0);

        string overridePath = "Animations/" + characterName + "Override";
        AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);

        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            Debug.LogError("Override Controller not found for " + characterName + " at " + overridePath);
        }
    }

    public void NextCharacter()
    {
        selectedOption++;
        if (selectedOption >= characterDB.CharacterCount)
        {
            selectedOption = 0;
        }
        UpdateCharacter(selectedOption);
        SaveCharacter();
    }

    public void PreviousCharacter()
    {
        selectedOption--;
        if (selectedOption < 0)
        {
            selectedOption = characterDB.CharacterCount - 1;
        }
        UpdateCharacter(selectedOption);
        SaveCharacter();
    }

    public void ConfirmSelection()
    {
        SaveCharacter();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReloadSelectedCharacters();
        }

        SceneManager.LoadScene("StoryMode");
    }

    public void SaveCharacter()
    {
        PlayerPrefs.SetInt("selectedStoryCharacter", selectedOption);
        // Save the state of the debug toggle so the battle scene can read it
        PlayerPrefs.SetInt("unlockAllAbilitiesDebug", unlockAllAbilitiesDebug ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadCharacter()
    {
        selectedOption = PlayerPrefs.GetInt("selectedStoryCharacter", 0);
    }
}