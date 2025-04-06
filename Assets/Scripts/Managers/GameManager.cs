using System;
using System.Collections;
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

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip battleMusic;


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

        string name = scene.name;

        if (name == "StartScreen" || name == "CharacterSelection")
        {
            PlayMusic(menuMusic);
        }
        else if (name == "QuizScene" || name == "BattleScene")
        {
            PlayMusic(battleMusic);
        }
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


    public int GetPlayerHealth(int player)
    {
        return player == 1 ? player1Health : player2Health;
    }

    public int DamagePlayer(int player, int damage)
    {
        int actualDamage = damage;

        if (player == 1)
        {
            actualDamage = Mathf.Min(player1Health, damage);
            player1Health = Mathf.Max(0, player1Health - damage);
        }
        else
        {
            actualDamage = Mathf.Min(player2Health, damage);
            player2Health = Mathf.Max(0, player2Health - damage);
        }

        return actualDamage;
    }

    public int HealPlayer(int playerNum, int amount)
    {
        int actualHealing = 0;

        if (playerNum == 1)
        {
            int oldHealth = player1Health;
            player1Health = Mathf.Min(player1Health + amount, SelectedCharacterP1.maxHealth);
            actualHealing = player1Health - oldHealth;
        }
        else if (playerNum == 2)
        {
            int oldHealth = player2Health;
            player2Health = Mathf.Min(player2Health + amount, SelectedCharacterP2.maxHealth);
            actualHealing = player2Health - oldHealth;
        }

        return actualHealing;
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