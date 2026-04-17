using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Tooltip("Vertical look limits in degrees: x = min pitch, y = max pitch (e.g. -90 to 90).")]
    [SerializeField] private Vector2 _minMaxRotationX = new Vector2(-90f, 90f);

    [Header("References")]
    [SerializeField] private Transform _camTransform;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private NetworkMovementComponent _networkMovementComponent;
    
    private PlayerControl _playerControl;
    private float _cameraAngle;

    public override void OnNetworkSpawn()
    {
        CinemachineCamera cam = _camTransform.gameObject.GetComponent<CinemachineCamera>();

        if (IsOwner)
        {
            cam.Priority = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            cam.Priority = 0;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Start()
    {
        _playerControl = new PlayerControl();
        _playerControl.Enable();
    }

    private void Update()
    {
        Vector2 movementInput = _playerControl.Player.Move.ReadValue<Vector2>();
        Vector2 lookInput = _playerControl.Player.Look.ReadValue<Vector2>();

        if (IsOwner)
        {
            _networkMovementComponent.ProcessLocalPlayerMovement(movementInput, lookInput);
        }
        else 
        {
            _networkMovementComponent.ProcessSimulatedPlayerMovement();
        }
    }

    /*
    private void RotateCamera(Vector2 lookInput)
    {
        _cameraAngle = Vector3.SignedAngle(transform.forward, _camTransform.forward, _camTransform.right);
        float cameraRotationAmount = lookInput.y * _turnSpeed * Time.deltaTime;
        float newCameraAngle = _cameraAngle - cameraRotationAmount;

        float minPitch = _minMaxRotationX.x;
        float maxPitch = _minMaxRotationX.y;
        if (minPitch > maxPitch)
        {
            (minPitch, maxPitch) = (maxPitch, minPitch);
        }

        if (newCameraAngle >= minPitch && newCameraAngle <= maxPitch)
        {
            _camTransform.Rotate(Vector3.right, -lookInput.y * _turnSpeed * Time.deltaTime);
        }
    }
    */
}
