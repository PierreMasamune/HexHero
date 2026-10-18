using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class HexSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private HexGridManager grid;
    [SerializeField] private LayerMask gridLayer;

    [Header("Hover Highlight (optional)")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private float highlightYOffset = 0.05f;

    [Header("Selection Highlight (optional)")]
    [SerializeField] private Transform selectionHighlight;

    [Header("State")]
    public HexCoord HoveredCell;
    public HexCoord SelectedCell;
    public bool HasHover { get; private set; }
    public bool HasSelection { get; private set; }

    public event System.Action<HexCoord> OnCellClicked;

    private void Reset()
    {
        cam = Camera.main;
        grid = FindAnyObjectByType<HexGridManager>();
    }

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindAnyObjectByType<HexGridManager>();

        if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 1. Check if a mouse exists
        if (Mouse.current == null) return;

        // 2. Read mouse position and raycast
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, gridLayer))
        {
            var cell = grid.WorldToCell(hit.point);

            if (grid.Contains(cell))
            {
                HoveredCell = cell;
                HasHover = true;

                if (hoverHighlight != null)
                {
                    hoverHighlight.gameObject.SetActive(true);
                    var p = grid.CellToWorld(cell);
                    hoverHighlight.position = new Vector3(p.x, p.y + highlightYOffset, p.z);
                }
            }
            else
            {
                // Raycast hit something, but it's outside the hex grid
                HasHover = false;
                if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
            }
        }
        else
        {
            // Raycast hit absolutely nothing
            HasHover = false;
            if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
        }

        // 3. Handle Clicking
        if (Mouse.current.leftButton.wasPressedThisFrame && HasHover)
        {
            SelectedCell = HoveredCell;
            HasSelection = true;

            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(true);
                var p = grid.CellToWorld(SelectedCell);
                selectionHighlight.position = new Vector3(p.x, p.y + highlightYOffset, p.z);
            }

            OnCellClicked?.Invoke(SelectedCell);
            Debug.Log($"Clicked cell: {SelectedCell}");
        }
    }
}