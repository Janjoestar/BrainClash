//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;

//public class LoadingScreenManager : MonoBehaviour
//{
//    [Header("References")]
//    public CharacterDatabase characterDB;
//    public SpriteRenderer characterArtwork;
//    public Text characterNameText;
//    public Text characterStoryText;
//    public Text loadingText;
//    public Button nextButton;
//    public Button previousButton;

//    [Header("Configuration")]
//    public float minLoadTime = 2.0f;
//    public float loadingTextBlinkRate = 0.5f;
//    public string sceneToLoad = "GameScene";

//    private int currentCharacterIndex = 0;
//    private Dictionary<string, string> characterStories = new Dictionary<string, string>();
//    private bool isLoadingComplete = false;

//    void Start()
//    {
//        // Set up character stories
//        InitializeCharacterStories();

//        // Load initial character
//        UpdateCharacterDisplay();

//        // Set up button listeners
//        nextButton.onClick.AddListener(NextCharacter);
//        previousButton.onClick.AddListener(PreviousCharacter);

//        // Start loading process
//        StartCoroutine(LoadGameSceneAsync());
//        StartCoroutine(AnimateLoadingText());
//    }

//    private void InitializeCharacterStories()
//    {
//        // Add character backstories
//        characterStories.Add("Samurai", "Once a royal guard, the Water Princess now wields her blade with grace and precision. " +
//            "Born in the Kingdom of Hydria, she trained under the guidance of the legendary Master Aquus. " +
//            "When darkness threatened her homeland, she discovered her latent ability to control water elemental powers. " +
//            "Now she travels the lands, seeking to restore balance to the elements while protecting the innocent.");

//        characterStories.Add("Ninja", "A shadow warrior from the hidden Mist Village, the Ninja mastered the arts of stealth and " +
//            "deception at a young age. After their clan was betrayed by one of their own, they became a lone agent of " +
//            "vengeance, using smoke bombs and lightning-fast attacks to confuse enemies. Their true identity remains a mystery, " +
//            "but their dedication to justice is unwavering.");

//        characterStories.Add("Wizard", "The eccentric Wizard was once a humble librarian who discovered an ancient tome of forgotten spells. " +
//            "Self-taught and unorthodox, they challenge the rigid traditions of the Mage Council with creative spell combinations " +
//            "and explosive results. Though sometimes their magic backfires spectacularly, their brilliant mind and determined " +
//            "spirit make them a formidable ally in battle.");

//        characterStories.Add("Robot", "Constructed in a laboratory by a genius inventor, Robot gained sentience and broke free " +
//            "from its programming constraints. Now exploring what it means to be alive, Robot's logical approach to battle " +
//            "is enhanced by its growing emotional intelligence. Its metal frame houses advanced weaponry and a spark of " +
//            "something the creator never intended: a soul.");

//        // Add more stories for your characters here
//        // For characters without explicit stories, provide a default
//    }

//    private void UpdateCharacterDisplay()
//    {
//        Character character = characterDB.GetCharacter(currentCharacterIndex);

//        // Update visuals
//        characterArtwork.sprite = character.characterSprite;
//        characterNameText.text = character.characterName;

//        // Set character's backstory
//        if (characterStories.ContainsKey(character.characterName))
//        {
//            characterStoryText.text = characterStories[character.characterName];
//        }
//        else
//        {
//            characterStoryText.text = "A mysterious fighter with untold powers. This warrior's past remains shrouded in mystery.";
//        }

//        // Apply character animations if available
//        ApplyCharacterAnimation(characterArtwork.gameObject, character.characterName);
//    }

//    public void NextCharacter()
//    {
//        currentCharacterIndex++;
//        if (currentCharacterIndex >= characterDB.CharacterCount)
//        {
//            currentCharacterIndex = 0;
//        }
//        UpdateCharacterDisplay();
//    }

//    public void PreviousCharacter()
//    {
//        currentCharacterIndex--;
//        if (currentCharacterIndex < 0)
//        {
//            currentCharacterIndex = characterDB.CharacterCount - 1;
//        }
//        UpdateCharacterDisplay();
//    }

//    private void ApplyCharacterAnimation(GameObject characterObject, string characterName)
//    {
//        Animator animator = characterObject.GetComponent<Animator>();
//        if (animator == null)
//        {
//            Debug.LogError("Animator component missing on " + characterObject.name);
//            return;
//        }

//        // Reset Animator to prevent glitches
//        animator.runtimeAnimatorController = null;
//        animator.Rebind();
//        animator.Update(0);

//        // Load Override Controller from Resources
//        string overridePath = "Animations/" + characterName + "Override";
//        AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);

//        if (overrideController != null)
//        {
//            animator.runtimeAnimatorController = overrideController;
//            // Set idle animation
//            animator.SetTrigger("Idle");
//        }
//        else
//        {
//            Debug.LogError("Override Controller not found for " + characterName + " at " + overridePath);
//        }
//    }

//    IEnumerator LoadGameSceneAsync()
//    {
//        // Start minimum load time
//        float startTime = Time.time;

//        // Start real scene loading
//        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

//        // Don't let the scene activate until we allow it
//        asyncLoad.allowSceneActivation = false;

//        // Update progress bar while loading
//        while (!asyncLoad.isDone)
//        {

//            // Check if loading is actually complete but we're waiting for minimum time
//            if (asyncLoad.progress >= 0.9f)
//            {
//                isLoadingComplete = true;

//                // Check if we've also waited the minimum time
//                if (Time.time - startTime >= minLoadTime)
//                {
//                    asyncLoad.allowSceneActivation = true;
//                }
//            }

//            yield return null;
//        }
//    }

//    IEnumerator AnimateLoadingText()
//    {
//        string baseText = "LOADING";
//        int dotCount = 0;

//        while (!isLoadingComplete || Time.time - minLoadTime < 0)
//        {
//            dotCount = (dotCount + 1) % 4;
//            string dots = new string('.', dotCount);
//            loadingText.text = baseText + dots;

//            yield return new WaitForSeconds(loadingTextBlinkRate);
//        }

//        loadingText.text = "PRESS ANY KEY TO CONTINUE";

//        // Wait for key press
//        while (true)
//        {
//            if (Input.anyKeyDown)
//            {
//                SceneManager.LoadScene(sceneToLoad);
//                break;
//            }
//            yield return null;
//        }
//    }
//}