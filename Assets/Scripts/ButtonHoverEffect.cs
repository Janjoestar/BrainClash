using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this component to any GameObject with a Button and Text component
/// to automatically add hover color effects
/// </summary>
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    [Header("Target Text (optional - will auto-find if not set)")]
    public Text targetText;

    private Button button;

    void Start()
    {
        // Get button component
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"ButtonHoverEffect requires a Button component on {gameObject.name}");
            enabled = false;
            return;
        }

        // Auto-find text component if not manually assigned
        if (targetText == null)
        {
            // First try to find Text component in children
            targetText = GetComponentInChildren<Text>();

            // If not found in children, try parent
            if (targetText == null)
            {
                targetText = GetComponentInParent<Text>();
            }

            if (targetText == null)
            {
                Debug.LogWarning($"No Text component found for ButtonHoverEffect on {gameObject.name}");
                enabled = false;
                return;
            }
        }

        // Set initial color
        targetText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetText != null && button.interactable)
        {
            targetText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetText != null)
        {
            targetText.color = normalColor;
        }
    }

    // Public method to change colors at runtime
    public void SetColors(Color normal, Color hover)
    {
        normalColor = normal;
        hoverColor = hover;

        if (targetText != null)
        {
            targetText.color = normalColor;
        }
    }
}