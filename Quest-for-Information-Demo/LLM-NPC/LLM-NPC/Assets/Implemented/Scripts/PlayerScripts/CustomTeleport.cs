using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Implements a custom teleporter, allowing ofr easy ray switches and mapping of teleport to a custom button.
/// One of 2 rays is active at each time - basic interaction ray (default) or teleport ray.
/// The teleport ray is activated by pressing the teleportInputAction button, and deactivasted by releasing the button. 
/// Deactivating causes the player to teleport (if they are teleporting to a teleport area)
/// </summary>.
public class CustomTeleport : MonoBehaviour
{
    [SerializeField]
    private XROrigin _headset;

    [SerializeField]
    private XRRayInteractor _teleportRayInteractor;

    [SerializeField]
    private TeleportationProvider _teleportationProvider;

    [SerializeField] 
    private ActionBasedController _currXrController;

    [SerializeField]
    private XRRayInteractor _currRayInteractor;

    [SerializeField]
    private InputActionReference _teleportInputAction;

    void Start()
    {
        _teleportRayInteractor.gameObject.SetActive(false);
    }
    void OnEnable()
    {
        _teleportInputAction.action.started += OnTeleportPressed;
        _teleportInputAction.action.canceled += OnTeleportReleased;
    }

    void OnDisable()
    {
        _teleportInputAction.action.started -= OnTeleportPressed;
        _teleportInputAction.action.canceled -= OnTeleportReleased;
    }

    // Called when the grip button is pressed
    private void OnTeleportPressed(InputAction.CallbackContext context)
    {
        _teleportRayInteractor.gameObject.SetActive(true);

        if (_currRayInteractor.interactablesSelected.Count == 0)
            _currRayInteractor.enabled = false;
    }

    // Called when the grip button is released
    private void OnTeleportReleased(InputAction.CallbackContext context)
    {
        _currRayInteractor.enabled = true;
        Teleport();
    }

    private void Teleport()
    {
        if (_teleportRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            TeleportationArea teleportationArea = hit.collider.GetComponent<TeleportationArea>();

            // Only proceed with teleport if the hit object has a TeleportationArea component
            if (teleportationArea != null)
            {
                //var teleportY = _headset.CameraYOffset;
                //destination.y = teleportY;
                var destination = hit.point;
                TeleportRequest request = new TeleportRequest()
                {
                    destinationPosition = destination,
                };
                _teleportationProvider.QueueTeleportRequest(request);
            }
        }
        _teleportRayInteractor.gameObject.SetActive(false);
    }
}
