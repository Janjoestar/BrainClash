// CombatTextManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatTextManager : MonoBehaviour
{
    private static CombatTextManager _instance;
    public static CombatTextManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CombatTextManager");
                _instance = go.AddComponent<CombatTextManager>();
                _instance.combatTextPrefab = Resources.Load<GameObject>("CombatTextPrefab");
                if (_instance.combatTextPrefab == null)
                {
                    Debug.LogError("CombatTextManager could not find 'CombatTextPrefab' in a Resources folder!");
                }
            }
            return _instance;
        }
    }

    public GameObject combatTextPrefab;

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(gameObject);
        else _instance = this;
    }

    // In CombatTextManager.cs

    public void ShowText(string text, Transform target, Color color, bool isCrit)
    {
        if (combatTextPrefab == null || Camera.main == null) return;

        GameObject textObj = Instantiate(combatTextPrefab, target.position + Vector3.up * 1.5f, Quaternion.identity, target);
        Text uiText = textObj.GetComponentInChildren<Text>();

        uiText.text = text;
        uiText.color = color;

        // --- FIX: Use scale for crits instead of font size ---
        if (isCrit)
        {
            // Instead of changing font size, we make the whole object bigger.
            textObj.transform.localScale *= 1.5f;
        }

        StartCoroutine(AnimateText(textObj.transform));
    }

    private IEnumerator AnimateText(Transform textTransform)
    {
        float duration = 1.5f;
        float speed = 1.0f;
        float fadeStart = 1.0f;
        float elapsed = 0f;
        Text uiText = textTransform.GetComponentInChildren<Text>();
        Color startColor = uiText.color;

        while (elapsed < duration)
        {
            if (textTransform == null) yield break;

            textTransform.position += Vector3.up * speed * Time.deltaTime;

            if (elapsed > fadeStart)
            {
                float alpha = 1.0f - ((elapsed - fadeStart) / (duration - fadeStart));
                uiText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (textTransform != null) Destroy(textTransform.gameObject);
    }
}