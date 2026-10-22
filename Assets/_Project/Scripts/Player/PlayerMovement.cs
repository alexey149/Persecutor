using Persecutor.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private const float MaxInputLength = 1f;
        private const float MinSquaredMoveLength = 0.01f;

        [SerializeField] private TraversalSettings _traversalSettings;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 720f;
        [SerializeField, Min(0f)] private float _groundSnapSpeed = 6f;

        private CharacterController _characterController;
        private float _verticalSpeed;

        public float MoveSpeed => _moveSpeed;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.stepOffset = _traversalSettings.StepHeight;
            _characterController.slopeLimit = _traversalSettings.SlopeLimit;
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _jumpAction.action.Disable();
        }

        private void Update()
        {
            Vector3 horizontalVelocity = CalculateHorizontalVelocity();

            UpdateVerticalSpeed();
            _characterController.Move((horizontalVelocity + Vector3.up * _verticalSpeed) * Time.deltaTime);
            RotateTowards(horizontalVelocity);
        }

        private Vector3 CalculateHorizontalVelocity()
        {
            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            Vector3 forward = _cameraTransform.forward.Flatten().normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, MaxInputLength);

            return direction * _moveSpeed;
        }

        private void UpdateVerticalSpeed()
        {
            if (_characterController.isGrounded == false)
            {
                _verticalSpeed -= _traversalSettings.Gravity * Time.deltaTime;

                return;
            }

            if (_jumpAction.action.WasPressedThisFrame())
            {
                _verticalSpeed = _traversalSettings.JumpSpeed;
            }
            else
            {
                _verticalSpeed = -_groundSnapSpeed;
            }
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < MinSquaredMoveLength)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
        }
    }
}
