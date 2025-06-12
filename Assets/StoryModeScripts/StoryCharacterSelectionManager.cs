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
        // Get only the starting attack for display in character selection
        List<AttackData> attacks = new List<AttackData>();
        AttackData startingAttack = storyAttackDataManager.GetStartingAttackForCharacter(characterName);
        if (startingAttack != null)
        {
            attacks.Add(startingAttack);
        }
        currentAttacks = attacks;
        UpdateAttackUI(attacks, attackButtons, attackNames);
    }

    private void UpdateAttackUI(List<AttackData> attacks, Button[] buttons, Text[] names)
    {
        if (buttons == null || names == null)
        {
            Debug.LogError("Attack UI elements not assigned in inspector");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            // Only show the first attack (the starting attack)
            if (i == 0 && attacks.Count > 0)
            {
                buttons[i].gameObject.SetActive(true);

                if (i < names.Length && names[i] != null)
                    names[i].text = attacks[0].attackName; // Use the first attack

                ColorBlock colors = buttons[i].colors;
                colors.normalColor = storyAttackDataManager.GetColorForAttackType(attacks[0].attackType); // Use new manager
                buttons[i].colors = colors;
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    public void PlayAttackAnimation(int attackIndex)
    {
        GameObject characterObject = artworkSprite.gameObject;
        Animator animator = characterObject.GetComponent<Animator>();
        string attackTrigger = "Attack1"; // Always trigger Attack1 animation for preview

        AttackData attackData = null;
        if (currentAttacks != null && currentAttacks.Count > 0)
            attackData = currentAttacks[0]; // Always use the first attack for preview data

        if (animator != null)
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
        PlayerPrefs.Save();
    }

    private void LoadCharacter()
    {
        selectedOption = PlayerPrefs.GetInt("selectedStoryCharacter", 0);
    }
}