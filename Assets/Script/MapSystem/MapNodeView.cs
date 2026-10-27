using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MapNodeState { Locked, Available, Visited, Current }

public class MapNodeView : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image iconBackground;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button button;

    [Header("State Colors")]
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color availableColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color visitedColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.15f, 1f);

    [Header("Type Icons (optional)")]
    [SerializeField] private Sprite fightIcon;
    [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite buffIcon;
    [SerializeField] private Sprite bossIcon;
    [SerializeField] private Sprite startIcon;

    public string NodeId { get; private set; }
    public MapNodeType Type { get; private set; }
    public MapNodeState State { get; private set; }

    private System.Action<string> onClick;

    public void Bind(MapNodeData node, MapNodeState state, System.Action<string> clickHandler)
    {
        NodeId = node.id;
        Type = node.type;
        State = state;
        onClick = clickHandler;

        if (iconImage != null)
        {
            var sprite = GetIconForType(node.type);
            if (sprite != null) { iconImage.sprite = sprite; iconImage.enabled = true; }
            else { iconImage.enabled = false; }
        }

        if (iconBackground != null)
            iconBackground.color = state switch
            {
                MapNodeState.Locked => lockedColor,
                MapNodeState.Available => availableColor,
                MapNodeState.Visited => visitedColor,
                MapNodeState.Current => currentColor,
                _ => Color.white
            };

        if (labelText != null)
        {
            string text = string.IsNullOrEmpty(node.label) ? node.type.ToString() : node.label;
            labelText.text = text;
        }

        if (button != null)
        {
            button.interactable = state == MapNodeState.Available;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        // Visited / current nodes are dimmed a bit
        if (State == MapNodeState.Visited || State == MapNodeState.Locked)
            transform.localScale = Vector3.one * 0.85f;
        else
            transform.localScale = Vector3.one;
    }

    private void HandleClick()
    {
        if (State != MapNodeState.Available) return;
        onClick?.Invoke(NodeId);
    }

    private Sprite GetIconForType(MapNodeType t) => t switch
    {
        MapNodeType.Fight => fightIcon,
        MapNodeType.Shop => shopIcon,
        MapNodeType.Buff => buffIcon,
        MapNodeType.Boss => bossIcon,
        MapNodeType.Start => startIcon,
        _ => null
    };
}