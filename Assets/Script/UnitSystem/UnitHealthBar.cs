using UnityEngine;
using TMPro;

public class UnitHealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave this empty. The script will automatically grab the Unit component from the parent.")]
    [SerializeField] private Unit unit;
    [SerializeField] private CurvedHealthBar curvedBar;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Visuals")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    [Header("Rotation Settings")]
    [Tooltip("Check this to make the UI always face the Main Camera. " +
             "Uncheck it to keep the rotation you set in the Editor.")]
    [SerializeField] private bool faceCamera = false;

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        // Automatically find the Unit script on the parent (the unit prefab)
        if (unit == null)
            unit = GetComponentInParent<Unit>();

        if (unit != null)
        {
            // Subscribe to the HP change event
            unit.OnHPChanged += UpdateBar;

            // Initial update
            UpdateBar(unit.CurrentHP, unit.MaxHP);
        }
        else
        {
            Debug.LogError("[UnitHealthBar] No Unit found on parent!");
        }
    }

    private void OnDestroy()
    {
        if (unit != null) unit.OnHPChanged -= UpdateBar;
    }

    private void Update()
    {
        if (unit == null || !unit.IsAlive)
        {
            if (unit != null && !unit.IsAlive) gameObject.SetActive(false);
            return;
        }

        // 1. Follow the unit's position
        transform.position = unit.transform.position + offset;

        // 2. Rotation Logic (Option 3)
        if (faceCamera && mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }

    private void UpdateBar(int current, int max)
    {
        if (curvedBar != null)
            curvedBar.SetHealth(current, max);

        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
            hpText.color = ((float)current / max <= 0.25f) ? Color.red : Color.white;
        }
    }
}