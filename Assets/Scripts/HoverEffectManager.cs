using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manager utility for applying hover effects to multiple buttons at once
/// Similar to your original implementation but as a reusable utility
/// </summary>
public class HoverEffectManager : MonoBehaviour
{
    [Header("Hover Effects")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    [Header("Size Effects")]
    public bool enableSizeEffect = true;
    public float normalScale = 1f;
    public float hoverScale = 1.1f;

    [Header("Target Texts")]
    public Text[] targetTexts;

    [Header("Auto-Setup Options")]
    [Tooltip("Automatically find all Text components in children and apply hover effects")]
    public bool autoSetupChildren = false;

    [Tooltip("Automatically find all Text components with specific tag")]
    public string targetTag = "HoverText";
    public bool useTaggedTexts = false;

    void Start()
    {
        SetupHoverEffects();
    }

    /// <summary>
    /// Initialize hover effects for all target texts
    /// </summary>
    public void SetupHoverEffects()
    {
        List<Text> textsToProcess = new List<Text>();

        // Add manually assigned texts
        if (targetTexts != null && targetTexts.Length > 0)
        {
            textsToProcess.AddRange(targetTexts);
        }

        // Auto-find children if enabled
        if (autoSetupChildren)
        {
            Text[] childTexts = GetComponentsInChildren<Text>();
            foreach (Text text in childTexts)
            {
                if (!textsToProcess.Contains(text))
                {
                    textsToProcess.Add(text);
                }
            }
        }

        // Find tagged texts if enabled
        if (useTaggedTexts && !string.IsNullOrEmpty(targetTag))
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (GameObject obj in taggedObjects)
            {
                Text text = obj.GetComponent<Text>();
                if (text != null && !textsToProcess.Contains(text))
                {
                    textsToProcess.Add(text);
                }
            }
        }

        // Apply hover effects to all collected texts
        foreach (Text text in textsToProcess)
        {
            AddHoverEffect(text);
        }

        Debug.Log($"HoverEffectManager: Applied hover effects to {textsToProcess.Count} text components");
    }

    /// <summary>
    /// Add hover effect to a specific text component
    /// </summary>
    /// <param name="buttonText">The text component to add hover effect to</param>
    public void AddHoverEffect(Text buttonText)
    {
        if (buttonText == null) return;

        Button button = buttonText.GetComponentInParent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"No Button component found in parent of {buttonText.name}");
            return;
        }

        // Set initial color and scale
        buttonText.color = normalColor;
        if (enableSizeEffect)
        {
            buttonText.transform.localScale = Vector3.one * normalScale;
        }

        // Add EventTrigger component if it doesn't exist
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        // Clear existing triggers to avoid duplicates
        trigger.triggers.Clear();

        // Create entry for pointer enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnButtonHover(buttonText, true); });
        trigger.triggers.Add(enterEntry);

        // Create entry for pointer exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnButtonHover(buttonText, false); });
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Handle hover state change
    /// </summary>
    /// <param name="buttonText">The text component to modify</param>
    /// <param name="isHovering">Whether the button is being hovered</param>
    public void OnButtonHover(Text buttonText, bool isHovering)
    {
        if (buttonText != null)
        {
            buttonText.color = isHovering ? hoverColor : normalColor;

            if (enableSizeEffect)
            {
                float targetScale = isHovering ? hoverScale : normalScale;
                buttonText.transform.localScale = Vector3.one * targetScale;
            }
        }
    }

    /// <summary>
    /// Update colors and scales for all managed texts at runtime
    /// </summary>
    /// <param name="newNormalColor">New normal color</param>
    /// <param name="newHoverColor">New hover color</param>
    /// <param name="newNormalScale">New normal scale</param>
    /// <param name="newHoverScale">New hover scale</param>
    public void UpdateEffects(Color newNormalColor, Color newHoverColor, float newNormalScale = 1f, float newHoverScale = 1.2f)
    {
        normalColor = newNormalColor;
        hoverColor = newHoverColor;
        normalScale = newNormalScale;
        hoverScale = newHoverScale;

        // Update all current text colors and scales to new normal values
        if (targetTexts != null)
        {
            foreach (Text text in targetTexts)
            {
                if (text != null)
                {
                    text.color = normalColor;
                    if (enableSizeEffect)
                    {
                        text.transform.localScale = Vector3.one * normalScale;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Add hover effect to a new text at runtime
    /// </summary>
    /// <param name="newText">Text component to add</param>
    public void AddNewText(Text newText)
    {
        if (newText == null) return;

        // Add to array if not already present
        if (targetTexts != null)
        {
            List<Text> textList = new List<Text>(targetTexts);
            if (!textList.Contains(newText))
            {
                textList.Add(newText);
                targetTexts = textList.ToArray();
            }
        }
        else
        {
            targetTexts = new Text[] { newText };
        }

        // Apply hover effect
        AddHoverEffect(newText);
    }

    /// <summary>
    /// Remove hover effect from a text component
    /// </summary>
    /// <param name="textToRemove">Text component to remove hover effect from</param>
    public void RemoveHoverEffect(Text textToRemove)
    {
        if (textToRemove == null) return;

        Button button = textToRemove.GetComponentInParent<Button>();
        if (button != null)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
        }

        // Reset color and scale
        textToRemove.color = normalColor;
        if (enableSizeEffect)
        {
            textToRemove.transform.localScale = Vector3.one * normalScale;
        }
    }
}