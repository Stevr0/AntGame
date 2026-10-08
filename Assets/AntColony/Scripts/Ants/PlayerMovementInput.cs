using UnityEngine;
using UnityEngine.InputSystem;

namespace AntColony.Ants
{
    /// <summary>
    /// Reads directional input for the player-controlled ant and forwards a
    /// normalised world-direction request to <see cref="AntSurfaceMotor"/>.
    ///
    /// This component decides what direction the player wants. It does not move
    /// the Rigidbody2D or decide which surfaces are reachable; those remain the
    /// motor's responsibility.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(AntSurfaceMotor))]
    public sealed class PlayerMovementInput : MonoBehaviour
    {
        [Header("Input Action")]
        [SerializeField] private string _movementActionName = "Move";

        private PlayerInput _playerInput;
        private AntSurfaceMotor _surfaceMotor;
        private InputAction _movementAction;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _surfaceMotor = GetComponent<AntSurfaceMotor>();

            // The existing Move action already combines WASD and arrow keys into
            // one Vector2. Its 2D composite also makes opposite directions cancel.
            _movementAction = _playerInput.actions?.FindAction(_movementActionName);

            if (_movementAction == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovementInput)} on '{name}' could not find the " +
                    $"'{_movementActionName}' input action.",
                    this);
            }
        }

        private void Update()
        {
            Vector2 rawIntent = _movementAction?.ReadValue<Vector2>() ?? Vector2.zero;

            // Preserve partial analogue-stick input, but cap keyboard diagonals at
            // length one so pressing two directions never produces extra speed.
            Vector2 normalisedIntent = Vector2.ClampMagnitude(rawIntent, 1f);
            _surfaceMotor.SetMovementIntent(normalisedIntent);
        }

        private void OnDisable()
        {
            // Clearing intent is important when input is released by disabling or
            // switching controllers. It also prevents the last direction sticking.
            if (_surfaceMotor != null)
            {
                _surfaceMotor.SetMovementIntent(Vector2.zero);
            }
        }
    }
}
