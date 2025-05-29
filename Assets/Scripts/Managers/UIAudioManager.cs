using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance;

    [Header("UI Sound Effects")]
    public AudioClip doneSound;
    public AudioClip clickSound;
    public AudioClip cancelSound;
    public AudioClip hoverSound;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;
    public float hoverDelay = 0.1f; // Delay for hover sound to prevent rapid triggering

    private AudioSource audioSource;
    private float lastGlobalHoverTime = 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configure audio source for UI sounds
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    void Start()
    {
        AddSoundsToAllButtons();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Small delay to ensure scene is fully loaded
        Invoke("AddSoundsToAllButtons", 0.1f);
    }

    void AddSoundsToAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        Debug.Log($"Found {allButtons.Length} buttons to add sounds to");

        foreach (Button button in allButtons)
        {
            AddButtonSounds(button);
        }
    }

    public void AddButtonSounds(Button button)
    {
        if (button == null) return;

        // Check if button already has specific sound methods assigned
        bool hasSpecificSound = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            string methodName = button.onClick.GetPersistentMethodName(i);
            if (methodName == "PlayCancelSound" || methodName == "PlayDoneSound" || methodName == "PlayClickSound")
            {
                hasSpecificSound = true;
                break;
            }
        }

        // Only add default click sound if no specific sound is already assigned
        if (!hasSpecificSound)
        {
            button.onClick.AddListener(() => PlayClickSound());
        }

        // Add hover sound using a custom component instead of EventTrigger
        UIHoverSound hoverComponent = button.GetComponent<UIHoverSound>();
        if (hoverComponent == null)
        {
            hoverComponent = button.gameObject.AddComponent<UIHoverSound>();
        }
    }

    // Public methods to play sounds
    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    public void PlayDoneSound()
    {
        PlaySound(doneSound);
    }

    public void PlayCancelSound()
    {
        PlaySound(cancelSound);
    }

    public void PlayHoverSound()
    {
        // Global hover delay check
        if (Time.time - lastGlobalHoverTime >= hoverDelay)
        {
            PlaySound(hoverSound);
            lastGlobalHoverTime = Time.time;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else if (clip == null)
        {
            Debug.LogWarning("Audio clip is null!");
        }
        else if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is null!");
        }
    }

    public void RefreshAllButtons()
    {
        AddSoundsToAllButtons();
    }
}

// Separate component for hover sounds that doesn't interfere with existing hover effects
public class UIHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayHoverSound();
        }
    }
}