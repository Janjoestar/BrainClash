using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static event Action OnGameManagerReady; // Event for when GameManager finishes loading characters

    [SerializeField] private float player1Health = 1;
    [SerializeField] private float player2Health = 1;

    private int lastCorrectPlayer = 0;

    public Character SelectedCharacterP1 { get; private set; }
    public Character SelectedCharacterP2 { get; private set; }
    public CharacterDatabase characterDB;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // Music arrays for different categories
    [SerializeField] private AudioClip[] menuMusic;        // For StartScreen and CharacterSelection
    [SerializeField] private AudioClip[] loadingMusic;     // For LoadingScene
    [SerializeField] private AudioClip[] gameplayMusic;    // For QuizScene and BattleScene

    // Keep track of current music category to avoid switching when moving between scenes of same category
    private string currentMusicCategory = "";

    public static float player1DamageMultiplier = 1;
    public static float player2DamageMultiplier = 1;

    private int playerDefeatedInQuiz = 0; // 0 = none, 1 = player1, 2 = player2

    private HashSet<int> usedQuestionIndices = new HashSet<int>();

    // Add these methods to GameManager class:
    public bool IsQuestionUsed(int questionIndex)
    {
        return usedQuestionIndices.Contains(questionIndex);
    }

    public void MarkQuestionAsUsed(int questionIndex)
    {
        usedQuestionIndices.Add(questionIndex);
    }

    public int GetUsedQuestionCount()
    {
        return usedQuestionIndices.Count;
    }

    public void ResetUsedQuestions()
    {
        usedQuestionIndices.Clear();
    }

    private Dictionary<int, float> battleDamageDealt = new Dictionary<int, float>() { { 1, 0f }, { 2, 0f } };
    private Dictionary<int, float> battleDamageTaken = new Dictionary<int, float>() { { 1, 0f }, { 2, 0f } };
    private Dictionary<int, float> battleHealingDone = new Dictionary<int, float>() { { 1, 0f }, { 2, 0f } };

    public void ResetBattleStats()
    {
        battleDamageDealt[1] = 0f;
        battleDamageDealt[2] = 0f;
        battleDamageTaken[1] = 0f;
        battleDamageTaken[2] = 0f;
        battleHealingDone[1] = 0f;
        battleHealingDone[2] = 0f;
    }

    public void AddDamageDealt(int player, float damage)
    {
        battleDamageDealt[player] += damage;
    }

    public void AddDamageTaken(int player, float damage)
    {
        battleDamageTaken[player] += damage;
    }

    public void AddHealingDone(int player, float healing)
    {
        battleHealingDone[player] += healing;
    }

    public float GetDamageDealt(int player)
    {
        return battleDamageDealt[player];
    }

    public float GetDamageTaken(int player)
    {
        return battleDamageTaken[player];
    }

    public float GetHealingDone(int player)
    {
        return battleHealingDone[player];
    }

    public void SetDefeatedPlayerInQuiz(int playerNumber)
    {
        playerDefeatedInQuiz = playerNumber;
    }

    public int GetDefeatedPlayerInQuiz()
    {
        int defeated = playerDefeatedInQuiz;
        playerDefeatedInQuiz = 0; // Reset after getting the value
        return defeated;
    }

    public void ResetDamageMultipliers()
    {
        player1DamageMultiplier = 1;
        player2DamageMultiplier = 1;
    }

    public void SetDoubleDamageForPlayer(int playerNumber, float damageMultiplier)
    {
        if (playerNumber == 1)
            player2DamageMultiplier += damageMultiplier;
        else if (playerNumber == 2)
            player1DamageMultiplier += damageMultiplier;
        else
        {
            Debug.Log("Player Number false");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Prevents GameManager from resetting
            SetupMusicSource();
            SceneManager.sceneLoaded += OnSceneLoaded; // Listen for scene changes
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

    private void SetupMusicSource()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.6f; // Music shouldn't drown out SFX
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 1.1f; // 🔊 Boost SFX volume here
        }
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

        // Determine which music category to use based on scene name
        string musicCategory = GetMusicCategoryForScene(scene.name);

        // Only change the music if we're moving to a different music category
        if (musicCategory != currentMusicCategory)
        {
            currentMusicCategory = musicCategory;

            switch (musicCategory)
            {
                case "menu":
                    PlayRandomMusic(menuMusic);
                    break;
                case "loading":
                    PlayRandomMusic(loadingMusic);
                    break;
                case "gameplay":
                    PlayRandomMusic(gameplayMusic);
                    break;
            }
        }
    }

    private string GetMusicCategoryForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "StartScreen":
            case "CharacterSelection":
                return "menu";
            case "LoadingScene":
                return "loading";
            case "QuizScene":
            case "BattleScene":
                return "gameplay";
            default:
                Debug.LogWarning("Unknown scene name: " + sceneName);
                return "menu"; // Default to menu music
        }
    }

    private void PlayRandomMusic(AudioClip[] musicClips)
    {
        if (musicClips == null || musicClips.Length == 0)
        {
            Debug.LogWarning("No music clips available for this category.");
            return;
        }

        // Select a random clip from the array
        AudioClip selectedClip = musicClips[UnityEngine.Random.Range(0, musicClips.Length)];
        PlayMusic(selectedClip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Music clip is null.");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(string sfxName, float volume = 1.0f, float musicDuckVolume = 0.2f)
    {
        if (string.IsNullOrEmpty(sfxName)) return;

        AudioClip clip = Resources.Load<AudioClip>("SFX/" + sfxName);
        if (clip != null)
        {
            StartCoroutine(DuckMusicThenPlaySFX(clip, volume, musicDuckVolume));
        }
        else
        {
            Debug.LogWarning("SFX not found: " + sfxName);
        }
    }

    private IEnumerator DuckMusicThenPlaySFX(AudioClip clip, float sfxVolume, float musicDuckVolume)
    {
        float originalMusicVolume = musicSource.volume;

        musicSource.volume = musicDuckVolume;

        sfxSource.PlayOneShot(clip, sfxVolume);

        yield return new WaitForSeconds(clip.length);

        musicSource.volume = originalMusicVolume;
    }

    public void SetLastCorrectPlayer(int playerNumber)
    {
        lastCorrectPlayer = playerNumber;
    }

    public int GetLastCorrectPlayer()
    {
        return lastCorrectPlayer;
    }

    public float GetPlayerHealth(int player)
    {
        return player == 1 ? player1Health : player2Health;
    }

    public float DamagePlayer(int player, float damage, bool applyDamageMultiplier)
    {
        float actualDamage;
        if (player == 2 && applyDamageMultiplier)
        {
            actualDamage = damage * player1DamageMultiplier;
        }
        else if (player == 1 && applyDamageMultiplier)
        {
            actualDamage = damage * player2DamageMultiplier;
        }
        else
            actualDamage = damage;

        if (player == 1)
        {
            actualDamage = Mathf.Min(player1Health, actualDamage); // Use actualDamage here
            player1Health = Mathf.Max(0, player1Health - actualDamage); // And here
        }
        else
        {
            actualDamage = Mathf.Min(player2Health, actualDamage); // Use actualDamage here
            player2Health = Mathf.Max(0, player2Health - actualDamage); // And here
        }
        return actualDamage;
    }

    public float HealPlayer(int playerNum, float amount)
    {
        float actualHealing = 0;

        if (playerNum == 1)
        {
            float oldHealth = player1Health;
            player1Health = Mathf.Min(player1Health + amount, SelectedCharacterP1.maxHealth);
            actualHealing = player1Health - oldHealth;
        }
        else if (playerNum == 2)
        {
            float oldHealth = player2Health;
            player2Health = Mathf.Min(player2Health + amount, SelectedCharacterP2.maxHealth);
            actualHealing = player2Health - oldHealth;
        }

        return actualHealing;
    }

    public void ReloadSelectedCharacters()
    {
        int selectedIndexP1 = PlayerPrefs.GetInt("selectedOptionP1", 0);
        int selectedIndexP2 = PlayerPrefs.GetInt("selectedOptionP2", 0);

        SelectedCharacterP1 = characterDB.GetCharacter(selectedIndexP1);
        SelectedCharacterP2 = characterDB.GetCharacter(selectedIndexP2);
        player1Health = SelectedCharacterP1.maxHealth;
        player2Health = SelectedCharacterP2.maxHealth;
    }

    public void ReturnToQuizScene()
    {
        player1DamageMultiplier = 1;
        player2DamageMultiplier = 1;
        SceneManager.LoadScene("QuizScene");
    }

    public void GoToBattleScene()
    {
        SceneManager.LoadScene("BattleScene");
    }
}