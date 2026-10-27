using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Play Area Bounds (world units)")]
    public Vector2 boundsMin = new Vector2(0f, -10f); // X/Z minimum
    public Vector2 boundsMax = new Vector2(0f, 5f);   // X/Z maximum

    [Header("Movement")]
    public float dragSensitivity = 1f;
    public bool invertDrag = false;
    public float smoothTime = 0.08f;

    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 12f;
    public bool zoomTowardsMouse = true;
    public float zoomSmoothTime = 0.08f;

    private Camera cam;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private float targetZoom;
    private float zoomVelocity;

    private bool isDragging;
    private Vector2 lastMousePos;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        targetPosition = transform.position;
        targetZoom = cam.orthographic ? cam.orthographicSize : -transform.position.z;
        ClampPositionToBounds();
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();

        // Smoothly move toward target
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref velocity, smoothTime);

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
        }
    }

    void HandleDrag()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool pressed = mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame;

        bool held = mouse.leftButton.isPressed
                 || mouse.rightButton.isPressed
                 || mouse.middleButton.isPressed;

        Vector2 screenPos = mouse.position.ReadValue();

        if (pressed)
        {
            lastMousePos = screenPos;
            isDragging = true;
        }

        if (!held)
        {
            isDragging = false;
            return;
        }

        if (!isDragging) return;

        // Calculate how much the mouse moved in screen space
        Vector2 mouseDelta = screenPos - lastMousePos;
        lastMousePos = screenPos;

        // Convert screen pixels to world units based on zoom level
        float worldUnitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;

        // Convert mouse movement to world movement (X and Z)
        Vector3 moveDelta = new Vector3(-mouseDelta.x, 0f, -mouseDelta.y) * worldUnitsPerPixel * dragSensitivity;

        if (invertDrag) moveDelta = -moveDelta;

        targetPosition += moveDelta;
        ClampPositionToBounds();
    }

    // This is now only used for zooming toward the mouse
    Vector3 GetWorldPointAtGround(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        // Ground plane at Y=0 where the table is
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        return transform.position;
    }

    void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float rawScroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(rawScroll, 0f)) return;

        float scroll = rawScroll / 120f;

        if (!cam.orthographic)
        {
            float zoomDelta = -scroll * zoomSpeed;
            Vector3 fwd = transform.forward;
            targetPosition += fwd * zoomDelta;
            ClampPositionToBounds();
            return;
        }

        if (zoomTowardsMouse)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 before = GetWorldPointAtGround(screenPos);

            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomSpeed, minZoom, maxZoom);
            cam.orthographicSize = targetZoom; // apply instantly for math

            Vector3 after = GetWorldPointAtGround(screenPos);

            targetPosition += before - after;
            ClampPositionToBounds();
        }
        else
        {
            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomSpeed, minZoom, maxZoom);
        }
    }

    void ClampPositionToBounds()
    {
        float halfHeight = cam.orthographic
            ? targetZoom
            : targetZoom * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * cam.aspect;

        float xMin = boundsMin.x + halfWidth;
        float xMax = boundsMax.x - halfWidth;
        float zMin = boundsMin.y + halfHeight;
        float zMax = boundsMax.y - halfHeight;

        float cx = (xMin > xMax) ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(targetPosition.x, xMin, xMax);
        float cz = (zMin > zMax) ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(targetPosition.z, zMin, zMax);

        targetPosition.x = cx;
        targetPosition.z = cz;
    }
}