using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkMovementComponent : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speed;
    [SerializeField] private float _turnSpeed;
    [Tooltip("Vertical look limits in degrees: x = min pitch, y = max pitch (e.g. -90 to 90).")]
    [SerializeField] private Vector2 _minMaxRotationX = new Vector2(-90f, 90f);

    [Header("References")]
    [SerializeField] private Transform _camParent;
    [SerializeField] private GameObject _cinemachineCam;
    [SerializeField] private CharacterController _characterController;

    private Transform _cinemachineCamTransform;

    private int _tick = 0;
    private float _tickRate = 1f / 60f;
    private float _tickDeltaTime = 0f;

    private const int BUFFER_SIZE = 1024; // how many inputs to buffer for client-side prediction and reconciliation
    private InputState[] _inputStates = new InputState[BUFFER_SIZE];
    private TransformState[] _transformStates = new TransformState[BUFFER_SIZE];

    public NetworkVariable<TransformState> serverTransformState = new NetworkVariable<TransformState>();
    public TransformState previousTransformState;

    private void OnEnable()
    {
        serverTransformState.OnValueChanged += OnServerTransformStateChanged;
    }

    public override void OnNetworkSpawn()
    {
        _cinemachineCamTransform = _cinemachineCam.transform;
    }

    private void OnServerTransformStateChanged(TransformState previousValue, TransformState newValue)
    {
        previousTransformState = previousValue;
    }

    public void ProcessLocalPlayerMovement(Vector2 movementInput, Vector2 lookInput)
    {
        _tickDeltaTime += Time.deltaTime;

        if (_tickDeltaTime > _tickRate)
        {
            int bufferIndex = _tick % BUFFER_SIZE;

            if (!IsServer)
            {
                MovePlayerServerRpc(_tick, movementInput, lookInput); // send input to server for processing
                // Immediately apply the input locally for client-side prediction for a more responsive feel.
                // The server will eventually send back the authoritative state, which we will reconcile with our predicted state.
                MovePlayer(movementInput);
                RotatePlayer(lookInput);
            }
            else
            {
                MovePlayer(movementInput);
                RotatePlayer(lookInput);

                TransformState state = new TransformState
                {
                    tick = _tick,
                    position = transform.position,
                    rotation = transform.rotation,
                    hasStartedMoving = true
                };

                previousTransformState = serverTransformState.Value; // store the previous state for reconciliation
                serverTransformState.Value = state; // update the server's authoritative state
            }

            InputState inputState = new InputState
            {
                tick = _tick,
                movementInput = movementInput,
                lookInput = lookInput
            };

            TransformState transformState = new TransformState
            {
                tick = _tick,
                position = transform.position,
                rotation = transform.rotation,
                hasStartedMoving = true
            };

            _inputStates[bufferIndex] = inputState; // buffer the input for potential reconciliation
            _transformStates[bufferIndex] = transformState; // buffer the transform for potential reconciliation

            _tickDeltaTime -= _tickRate; // reset tick delta time, but keep any leftover time to maintain accurate timing
            _tick++;
        }
    }

    public void ProcessSimulatedPlayerMovement()
    {
        _tickDeltaTime += Time.deltaTime;
        if (_tickDeltaTime > _tickRate)
        {
            TransformState authoritative = serverTransformState.Value;
            if (authoritative != null && authoritative.hasStartedMoving)
            {
                transform.position = authoritative.position;
                transform.rotation = authoritative.rotation;
            }

            _tickDeltaTime -= _tickRate; // reset tick delta time, but keep any leftover time to maintain accurate timing
            _tick++;
        }
    }

    private void MovePlayer(Vector2 movementInput)
    {
        Vector3 movement = movementInput.x * _cinemachineCamTransform.right + movementInput.y * _cinemachineCamTransform.forward;

        movement.y = 0; // prevent vertical movement

        if (!_characterController.isGrounded) // simple gravity
        {
            movement.y = -9.81f;
        }
        
        _characterController.Move(movement * _speed * _tickRate); // use fixed tick rate for consistent movement across clients and server
    }

    private void RotatePlayer(Vector2 lookInput)
    {
        /*
        float minPitch = _minMaxRotationX.x;
        float maxPitch = _minMaxRotationX.y;
        if (minPitch > maxPitch)
        {
            (minPitch, maxPitch) = (maxPitch, minPitch);
        }

        float pitchAngle = Vector3.SignedAngle(transform.forward, _cinemachineCamTransform.forward, _cinemachineCamTransform.right);
        float pitchDelta = lookInput.y * _turnSpeed * _tickRate;
        float newPitchAngle = pitchAngle - pitchDelta;

        if (newPitchAngle >= minPitch && newPitchAngle <= maxPitch)
        {
            _cinemachineCamTransform.Rotate(_cinemachineCamTransform.right, -lookInput.y * _turnSpeed * _tickRate);
        }
        */

        transform.Rotate(Vector3.up, lookInput.x * _turnSpeed * _tickRate);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(int tick, Vector2 movementInput, Vector2 lookInput)
    {
        /*
        if (_tick != previousTransformState.tick + 1)
        {
            // Missing message, so server state is out of sync with client.
            // In a production game, you would want to handle this case more robustly
            // (e.g., by requesting a state update from the client
            // or implementing a more sophisticated reconciliation strategy). 
        }
        */

        MovePlayer(movementInput);
        RotatePlayer(lookInput);

        TransformState state = new TransformState
        {
            tick = tick,
            position = transform.position,
            rotation = transform.rotation,
            hasStartedMoving = true
        };

        previousTransformState = serverTransformState.Value; // store the previous state for reconciliation
        serverTransformState.Value = state;
    }
}
