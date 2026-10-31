using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineInputAxisController))]
public class CameraToggle : MonoBehaviour
{
    [Tooltip("The key you must hold to move the camera")]
    public Key toggleKey = Key.C;

    private CinemachineInputAxisController inputController;

    private void Awake()
    {
        // Get the controller that handles the mouse input
        inputController = GetComponent<CinemachineInputAxisController>();
    }

    private void Update()
    {
        if (Keyboard.current == null || inputController == null) return;

        // Check if the C key is currently pressed down
        bool isHolding = Keyboard.current[toggleKey].isPressed;

        // Enable the component if C is held, otherwise disable it
        // This completely blocks Cinemachine from reading the mouse
        inputController.enabled = isHolding;
    }
}