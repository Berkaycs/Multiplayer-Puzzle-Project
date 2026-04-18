using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Tooltip("Vertical look limits in degrees: x = min pitch, y = max pitch (e.g. -90 to 90).")]
    [SerializeField] private Vector2 _minMaxRotationX = new Vector2(-90f, 90f);
    [SerializeField] private float _speed;
    [SerializeField] private float _turnSpeed;

    [Header("References")]
    [SerializeField] private Transform _camTransform;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private NetworkMovementComponent _networkMovementComponent;
    [SerializeField] private float _interactDistance;
    [SerializeField] private LayerMask _interactionLayer;
    
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
            //_networkMovementComponent.ProcessLocalPlayerMovement(movementInput, lookInput);

            if (_playerControl.Player.Move.inProgress)
            {
                Vector3 movement = movementInput.x * _camTransform.right + movementInput.y * _camTransform.forward;

                movement.y = 0f;

                _characterController.Move(movement * _speed * Time.deltaTime);
            }

            if (_playerControl.Player.Interact.inProgress)
            {
                if (Physics.Raycast(_camTransform.position, _camTransform.forward, out RaycastHit hit, _interactDistance, _interactionLayer))
                {
                    if (hit.collider.TryGetComponent<ButtonDoor>(out ButtonDoor buttonDoor))
                    {
                        UseButtonServerRpc();
                    }
                }
            }

            RotatePlayer(lookInput);
        }
        /*
        else 
        {
            _networkMovementComponent.ProcessSimulatedPlayerMovement();
        }
        */
    }

    [Rpc(SendTo.Server)]
    private void UseButtonServerRpc()
    {
        if (Physics.Raycast(_camTransform.position, _camTransform.forward, out RaycastHit hit, _interactDistance, _interactionLayer))
        {
            if (hit.collider.TryGetComponent<ButtonDoor>(out ButtonDoor buttonDoor))
            {
                buttonDoor.Activate();
            }
        }
    }

    private void RotatePlayer(Vector2 lookInput)
    {
        transform.Rotate(Vector3.up, lookInput.x * _turnSpeed);
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
