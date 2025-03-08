using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public string tooltipText;
    private static GameObject tooltipObj;
    private static Text tooltipTextComponent;
    private static RectTransform tooltipRectTransform;

    private void Start()
    {
        if (tooltipObj == null)
        {
            // Create tooltip panel
            tooltipObj = new GameObject("Tooltip");
            tooltipObj.transform.SetParent(GameObject.Find("Canvas").transform, false);
            tooltipObj.AddComponent<CanvasRenderer>();

            Image background = tooltipObj.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.8f);

            // Create text element
            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipObj.transform, false);
            tooltipTextComponent = textObj.AddComponent<Text>();
            tooltipTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipTextComponent.fontSize = 14;
            tooltipTextComponent.color = Color.white;
            tooltipTextComponent.alignment = TextAnchor.MiddleCenter;

            // Set up RectTransforms
            tooltipRectTransform = tooltipObj.GetComponent<RectTransform>();
            tooltipRectTransform.sizeDelta = new Vector2(200, 50); // Tooltip box size
            tooltipRectTransform.pivot = new Vector2(0, 1); // Pivot at the top-left

            RectTransform textRT = tooltipTextComponent.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 5);
            textRT.offsetMax = new Vector2(-5, -5);

            tooltipObj.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltipTextComponent.text = tooltipText;
        tooltipObj.SetActive(true);
        UpdateTooltipPosition(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipObj.SetActive(false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateTooltipPosition(eventData);
    }

    private void UpdateTooltipPosition(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform canvasRect = GameObject.Find("Canvas").GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            tooltipRectTransform.anchoredPosition = localPoint + new Vector2(15, -15); // Offset from cursor
        }
    }
}
