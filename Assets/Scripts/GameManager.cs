using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static event Action OnGameManagerReady; // Event for when GameManager finishes loading characters

    [SerializeField] private int player1Health = 100;
    [SerializeField] private int player2Health = 100;

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

        OnGameManagerReady?.Invoke(); // Notify BattleManager that characters are ready
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

    public void ReturnToQuizScene()
    {
            SceneManager.LoadScene("QuizScene");
    }

    public void GoToBattleScene()
    {
            SceneManager.LoadScene("BattleScene");
    }
}