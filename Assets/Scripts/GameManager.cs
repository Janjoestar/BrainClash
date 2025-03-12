using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private int player1Health = 100;
    [SerializeField] private int player2Health = 100;

    private int lastCorrectPlayer = 0;

    public Character SelectedCharacterP1 { get; private set; }
    public Character SelectedCharacterP2 { get; private set; }
    public CharacterDatabase characterDB;

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
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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

    public void DamagePlayer(int playerNumber, int damage)
    {
        if (playerNumber == 1)
            player1Health -= damage;
        else
            player2Health -= damage;

        // Check for game over
        if (player1Health <= 0 || player2Health <= 0)
        {
            // Handle game over
            int winner = player1Health <= 0 ? 2 : 1;

            // Reset health for a new game
            player1Health = 100;
            player2Health = 100;

            // Return to quiz scene to start a new game
            ReturnToQuizScene();
        }
    }

    public int GetPlayerHealth(int playerNumber)
    {
        return playerNumber == 1 ? player1Health : player2Health;
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