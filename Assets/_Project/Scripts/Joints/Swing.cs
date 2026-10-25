using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor.Joints
{
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public class Swing : MonoBehaviour
    {
        private const float MinSquaredSpeed = 0.01f;

        [SerializeField] private InteractionRange _interactionRange;
        [SerializeField] private InputActionReference _pushAction;
        [SerializeField, Min(0f)] private float _pushImpulse = 4f;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            _pushAction.action.performed += OnPush;
            _pushAction.action.Enable();
        }

        private void OnDisable()
        {
            _pushAction.action.performed -= OnPush;
            _pushAction.action.Disable();
        }

        private void OnPush(InputAction.CallbackContext context)
        {
            if (_interactionRange.IsPlayerInRange() == false)
            {
                return;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 direction = velocity.sqrMagnitude > MinSquaredSpeed ? velocity.normalized : transform.forward;

            _rigidbody.AddForce(direction * _pushImpulse, ForceMode.Impulse);
        }
    }
}
