using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// Manages the menu on the players left controller, that enables the player to return to the loby and restart the game/
/// Toggles the menu visibility.
/// </summary>
public class ControllerMenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _menuCanvas;

    [SerializeField]
    private InputActionReference _inputActionRef;


    private void Awake()
    {
        _inputActionRef.action.Enable();
        _inputActionRef.action.performed += ToggleMenu;
    }

    private void OnDestroy()
    {
        _inputActionRef.action.Disable();
        _inputActionRef.action.performed -= ToggleMenu;
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        _menuCanvas.SetActive(!_menuCanvas.activeSelf);
    }
}
