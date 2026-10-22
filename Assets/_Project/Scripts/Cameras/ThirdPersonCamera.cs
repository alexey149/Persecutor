using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor.Cameras
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private InputActionReference _lookAction;
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 2f, 0f);
        [SerializeField, Min(0f)] private float _distance = 8f;
        [SerializeField, Min(0f)] private float _sensitivity = 0.15f;
        [SerializeField] private float _minPitch = -10f;
        [SerializeField] private float _maxPitch = 70f;
        [SerializeField] private float _startPitch = 20f;

        private float _yaw;
        private float _pitch;

        private void OnEnable()
        {
            _lookAction.action.Enable();
        }

        private void OnDisable()
        {
            _lookAction.action.Disable();
        }

        private void Start()
        {
            _yaw = _target.eulerAngles.y;
            _pitch = _startPitch;
            LockCursor();
        }

        private void LateUpdate()
        {
            UpdateCursorLock();

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Rotate();
            }

            FollowTarget();
        }

        private void UpdateCursorLock()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }
        }

        private void Rotate()
        {
            Vector2 look = _lookAction.action.ReadValue<Vector2>() * _sensitivity;
            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, _minPitch, _maxPitch);
        }

        private void FollowTarget()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 position = _target.position + _pivotOffset - rotation * Vector3.forward * _distance;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
