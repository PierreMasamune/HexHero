using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class CurvedHealthBar : MaskableGraphic
{
    [Header("Curve Settings")]
    public float radius = 200f;
    public float thickness = 15f;
    public float startAngle = 15f;
    public float endAngle = 165f;
    public int segments = 30;

    [Header("State")]
    [Range(0f, 1f)]
    public float fillAmount = 1f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Color fullColor = new Color(0.7f, 0.85f, 0.3f);
    public Color midColor = new Color(1f, 0.7f, 0f);
    public Color lowColor = Color.red;
    public Color tipColor = Color.white;

    [Header("Stroke (Border)")]
    public Color strokeColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    [Tooltip("How thick the border is. Set to 0 to remove.")]
    public float strokeThickness = 4f;

    public void SetHealth(float current, float max)
    {
        fillAmount = Mathf.Clamp01(current / max);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float angleStep = (endAngle - startAngle) / segments;
        Color currentFillColor = fillAmount > 0.5f ? fullColor : (fillAmount > 0.25f ? midColor : lowColor);

        // 1. Draw the STROKE (Border) - Drawn first so it sits behind everything
        if (strokeThickness > 0)
        {
            for (int i = 0; i < segments; i++)
            {
                float a1 = (startAngle + (i * angleStep)) * Mathf.Deg2Rad;
                float a2 = (startAngle + ((i + 1) * angleStep)) * Mathf.Deg2Rad;
                // Pass a thicker version of the bar
                DrawSegment(vh, a1, a2, strokeColor, thickness + (strokeThickness * 2f));
            }
        }

        // 2. Draw the BACKGROUND (Dark Track)
        for (int i = 0; i < segments; i++)
        {
            float a1 = (startAngle + (i * angleStep)) * Mathf.Deg2Rad;
            float a2 = (startAngle + ((i + 1) * angleStep)) * Mathf.Deg2Rad;
            DrawSegment(vh, a1, a2, backgroundColor, thickness);
        }

        // 3. Draw the FILL (Colored Health)
        int fillSegments = Mathf.FloorToInt(segments * fillAmount);
        for (int i = 0; i < fillSegments; i++)
        {
            float a1 = (startAngle + (i * angleStep)) * Mathf.Deg2Rad;
            float a2 = (startAngle + ((i + 1) * angleStep)) * Mathf.Deg2Rad;
            DrawSegment(vh, a1, a2, currentFillColor, thickness);
        }

        // 4. Draw the GLOWING TIP
        if (fillAmount > 0.02f && fillAmount < 0.98f)
        {
            float currentAngle = Mathf.Lerp(startAngle, endAngle, fillAmount) * Mathf.Deg2Rad;
            DrawTip(vh, currentAngle, tipColor);
        }
    }

    private void DrawSegment(VertexHelper vh, float angle1, float angle2, Color color, float customThickness)
    {
        Vector2 dir1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));
        Vector2 dir2 = new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2));

        // Use the custom thickness to calculate inner and outer radii
        Vector2 inner1 = dir1 * (radius - customThickness / 2f);
        Vector2 outer1 = dir1 * (radius + customThickness / 2f);
        Vector2 inner2 = dir2 * (radius - customThickness / 2f);
        Vector2 outer2 = dir2 * (radius + customThickness / 2f);

        int startIndex = vh.currentVertCount;

        vh.AddVert(inner1, color, Vector2.zero);
        vh.AddVert(outer1, color, Vector2.zero);
        vh.AddVert(outer2, color, Vector2.zero);
        vh.AddVert(inner2, color, Vector2.zero);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private void DrawTip(VertexHelper vh, float currentAngle, Color color)
    {
        Vector2 dir = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
        float tipOffset = 0.5f * Mathf.Deg2Rad;
        Vector2 dirBack = new Vector2(Mathf.Cos(currentAngle - tipOffset), Mathf.Sin(currentAngle - tipOffset));

        Vector2 innerFront = dir * (radius - thickness / 2f);
        Vector2 outerFront = dir * (radius + thickness / 2f);
        Vector2 innerBack = dirBack * (radius - thickness / 2f);
        Vector2 outerBack = dirBack * (radius + thickness / 2f);

        int startIndex = vh.currentVertCount;

        vh.AddVert(innerFront, color, Vector2.zero);
        vh.AddVert(outerFront, color, Vector2.zero);
        vh.AddVert(outerBack, color, Vector2.zero);
        vh.AddVert(innerBack, color, Vector2.zero);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
}