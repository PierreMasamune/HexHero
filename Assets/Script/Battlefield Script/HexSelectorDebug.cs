using UnityEngine;
using UnityEngine.InputSystem;

public class HexSelectorDebug: MonoBehaviour
{
    public Camera cam;
    public LayerMask gridLayer;

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (Mouse.current == null)
        {
            Debug.Log("Mouse.current is null! Is the Input System setup correctly?");
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        }
        else
        {
            Debug.Log("Hit NOTHING");
        }
    }
}