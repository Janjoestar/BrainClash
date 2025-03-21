using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static event Action OnGameManagerReady; // Event for when GameManager finishes loading characters

    [SerializeField] private int player1Health = 1;
    [SerializeField] private int player2Health = 1;

    private int lastCorrectPlayer = 0;

    public Character SelectedCharacterP1 { get; private set; }
    public Character SelectedCharacterP2 { get; private set; }
    public CharacterDatabase characterDB;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Prevents GameManager from resetting
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadSelectedCharacters();
    }

    private void LoadSelectedCharacters()
    {
        int selectedIndexP1 = PlayerPrefs.GetInt("selectedOptionP1", 0);
        int selectedIndexP2 = PlayerPrefs.GetInt("selectedOptionP2", 0);

        SelectedCharacterP1 = characterDB.GetCharacter(selectedIndexP1);
        SelectedCharacterP2 = characterDB.GetCharacter(selectedIndexP2);
        player1Health = SelectedCharacterP1.maxHealth;
        player2Health = SelectedCharacterP2.maxHealth;


        OnGameManagerReady?.Invoke(); // Notify BattleManager that characters are ready
                                      // Force reapply animations after loading to fix glitches
        if (SceneManager.GetActiveScene().name == "CharacterSelectionScene")
        {
            CharacterSelectionManager selectionManager = FindObjectOfType<CharacterSelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.ApplyCharacterAnimation(selectionManager.artworkSpriteP1.gameObject, SelectedCharacterP1.characterName);
                selectionManager.ApplyCharacterAnimation(selectionManager.artworkSpriteP2.gameObject, SelectedCharacterP2.characterName);
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply animations when the QuizScene or BattleScene loads
        if (scene.name == "QuizScene")
        {
            QuizManager quizManager = FindObjectOfType<QuizManager>();
            if (quizManager != null)
            {
                quizManager.ApplyCharacterAnimation(quizManager.Player1, SelectedCharacterP1.characterName);
                quizManager.ApplyCharacterAnimation(quizManager.Player2, SelectedCharacterP2.characterName);
            }
        }
        else if (scene.name == "BattleScene")
        {
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null)
            {
                battleManager.ApplyCharacterAnimation(battleManager.Player1, SelectedCharacterP1.characterName);
                battleManager.ApplyCharacterAnimation(battleManager.Player2, SelectedCharacterP2.characterName);
            }
        }
    }

    public void SetLastCorrectPlayer(int playerNumber)
    {
        lastCorrectPlayer = playerNumber;
    }

    public int GetLastCorrectPlayer()
    {
        return lastCorrectPlayer;
    }


    public int GetPlayerHealth(int player)
    {
        return player == 1 ? player1Health : player2Health;
    }

    public void DamagePlayer(int player, int damage)
    {
        if (player == 1)
            player1Health -= damage;
        else
            player2Health -= damage;
    }

    public void HealPlayer(int playerNum, int amount)
    {
        if (playerNum == 1)
        {
            player1Health = Mathf.Min(player1Health + amount, SelectedCharacterP1.maxHealth);
        }
        else if (playerNum == 2)
        {
            player2Health = Mathf.Min(player2Health + amount, SelectedCharacterP2.maxHealth);
        }
    }

    public void ReturnToQuizScene()
    {
            SceneManager.LoadScene("QuizScene");
    }

    public void GoToBattleScene()
    {
            SceneManager.LoadScene("BattleScene");
    }
}