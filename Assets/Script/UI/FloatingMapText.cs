using UnityEngine;
using TMPro;

public class FloatingMapText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textUI;
    [SerializeField] private float floatSpeed = 50f;
    [SerializeField] private float lifetime = 1.5f;

    private float timer;
    private Color startColor;

    public static void Spawn(Transform parent, Vector2 position, string text, Color color)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/MapFloatingText");
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;

        var comp = go.GetComponent<FloatingMapText>();
        if (comp != null) comp.Setup(text, color);
    }

    private void Setup(string text, Color color)
    {
        if (textUI == null) textUI = GetComponent<TextMeshProUGUI>();
        textUI.text = text;
        textUI.color = color;
        startColor = color;
        timer = lifetime;
    }

    private void Update()
    {
        transform.localPosition += Vector3.up * floatSpeed * Time.deltaTime;
        timer -= Time.deltaTime;
        if (timer <= 0) Destroy(gameObject);
        else textUI.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Clamp01(timer / lifetime));
    }
}