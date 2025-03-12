//using UnityEngine;
//using UnityEngine.SceneManagement;
//using System.Collections;
//using UnityEngine.UI;

//public class TransitionManager : MonoBehaviour
//{
//    // Singleton instance
//    public static TransitionManager Instance { get; private set; }

//    [SerializeField] private GameObject transitionOverlay;
//    [SerializeField] private float transitionDuration = 1.0f;

//    private bool isTransitioning = false;

//    private void Awake()
//    {
//        // Set up singleton pattern
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);

//            // Create transition overlay if it doesn't exist
//            if (transitionOverlay == null)
//            {
//                CreateTransitionOverlay();
//            }
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }

//    private void CreateTransitionOverlay()
//    {
//        if (transitionOverlay != null) return;
//            // Create a new Canvas for our overlay
//            GameObject canvasObject = new GameObject("TransitionCanvas");
//        Canvas canvas = canvasObject.AddComponent<Canvas>();
//        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//        canvas.sortingOrder = 100; // Make sure it's on top of everything
//        canvasObject.AddComponent<CanvasScaler>();
//        canvasObject.AddComponent<GraphicRaycaster>();

//        // Create the overlay panel
//        GameObject panel = new GameObject("TransitionOverlay");
//        panel.transform.SetParent(canvasObject.transform, false);
//        Image image = panel.AddComponent<Image>();
//        image.color = Color.black;

//        // Set it to fill the screen
//        RectTransform rectTransform = panel.GetComponent<RectTransform>();
//        rectTransform.anchorMin = Vector2.zero;
//        rectTransform.anchorMax = Vector2.one;
//        rectTransform.sizeDelta = Vector2.zero;

//        // Set as our transition overlay
//        transitionOverlay = panel;

//        // Initially hide it
//        transitionOverlay.SetActive(false);

//        // Make the canvas persistent
//        DontDestroyOnLoad(canvasObject);
//    }

//    public void TransitionToScene(string sceneName)
//    {
//        // Prevent multiple transitions at once
//        if (isTransitioning)
//        {
//            Debug.LogWarning("Already transitioning to a scene");
//            return;
//        }

//        StartCoroutine(PerformTransition(sceneName));
//    }

//    private IEnumerator PerformTransition(string sceneName)
//    {
//        isTransitioning = true;

//        // Make sure the overlay exists
//        if (transitionOverlay == null)
//        {
//            CreateTransitionOverlay();
//        }

//        // Show the overlay
//        transitionOverlay.SetActive(true);
//        UnityEngine.UI.Image overlayImage = transitionOverlay.GetComponent<UnityEngine.UI.Image>();

//        // Fade in
//        float timer = 0;
//        while (timer < transitionDuration / 2)
//        {
//            timer += Time.deltaTime;
//            float alpha = Mathf.Clamp01(timer / (transitionDuration / 2));
//            overlayImage.color = new Color(0, 0, 0, alpha);
//            yield return null;
//        }

//        // Load the scene
//        Debug.Log("Loading scene: " + sceneName);

//        // Make sure the scene exists in the build
//        if (IsSceneInBuild(sceneName))
//        {
//            SceneManager.LoadScene(sceneName);
//        }
//        else
//        {
//            Debug.LogError("Scene " + sceneName + " is not in the build settings!");
//            isTransitioning = false;
//            overlayImage.color = new Color(0, 0, 0, 0);
//            transitionOverlay.SetActive(false);
//            yield break;
//        }

//        // Wait one frame for the scene to load
//        yield return null;

//        // Fade out
//        timer = 0;
//        while (timer < transitionDuration / 2)
//        {
//            timer += Time.deltaTime;
//            float alpha = 1 - Mathf.Clamp01(timer / (transitionDuration / 2));
//            overlayImage.color = new Color(0, 0, 0, alpha);
//            yield return null;
//        }

//        // Hide the overlay
//        overlayImage.color = new Color(0, 0, 0, 0);
//        transitionOverlay.SetActive(false);

//        isTransitioning = false;
//        Debug.Log("Transition to " + sceneName + " complete");
//    }

//    private bool IsSceneInBuild(string sceneName)
//    {
//        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
//        {
//            string path = SceneUtility.GetScenePathByBuildIndex(i);
//            string name = path.Substring(path.LastIndexOf('/') + 1).Replace(".unity", "");

//            if (name == sceneName)
//                return true;
//        }
//        return false;
//    }
//}