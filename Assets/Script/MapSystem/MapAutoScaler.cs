using UnityEngine;

public class MapAutoScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your NodeContainer here")]
    [SerializeField] private RectTransform content;
    [Tooltip("Drag your MapPaper here")]
    [SerializeField] private RectTransform viewport;

    [Header("Settings")]
    [SerializeField] private float padding = 50f; // Leaves a little gap around the edges

    private void Start()
    {
        FitToScreen();
    }

    // Call this if you resize the window or want to reset the zoom
    public void FitToScreen()
    {
        if (content == null || viewport == null) return;

        // Calculate how much we need to scale down to fit inside the paper
        float widthRatio = (viewport.rect.width - padding) / content.rect.width;
        float heightRatio = (viewport.rect.height - padding) / content.rect.height;

        // Use the smaller ratio so it fits perfectly inside both width and height
        float scale = Mathf.Min(widthRatio, heightRatio);

        // Apply the scale to the NodeContainer
        content.localScale = new Vector3(scale, scale, 1f);

        // Re-center the map
        content.anchoredPosition = Vector2.zero;
    }
}