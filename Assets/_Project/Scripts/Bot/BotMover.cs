using Persecutor.Movement;
using UnityEngine;

namespace Persecutor.Bot
{
    [RequireComponent(typeof(Rigidbody))]
    public class BotMover : MonoBehaviour
    {
        private const float GroundTolerance = 0.05f;
        private const float GroundSnapSmoothing = 0.5f;
        private const float MinSquaredDirectionLength = 0.0001f;

        [SerializeField] private TraversalSettings _traversalSettings;
        [SerializeField] private LayerMask _environmentMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;
        [SerializeField, Min(0f)] private float _obstacleCheckDistance = 1f;
        [SerializeField, Min(0f)] private float _jumpClearanceCheckDistance = 1.5f;

        private Rigidbody _rigidbody;
        private GroundSensor _groundSensor;
        private JumpObstacleSensor _jumpObstacleSensor;
        private bool _isGrounded;
        private bool _isJumping;

        public Vector3 Position => _rigidbody.position;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _groundSensor = new GroundSensor(_traversalSettings, _environmentMask);
            _jumpObstacleSensor = new JumpObstacleSensor(_traversalSettings, _environmentMask, _obstacleCheckDistance, _jumpClearanceCheckDistance);
        }

        public void Move(Vector3 horizontalVelocity, float deltaTime)
        {
            Vector3 feetPosition = _rigidbody.position;
            float verticalSpeed = _rigidbody.linearVelocity.y;

            UpdateJumpState(verticalSpeed);
            UpdateGroundState(feetPosition, out RaycastHit groundHit);

            if (_isGrounded)
            {
                _rigidbody.linearVelocity = CalculateGroundVelocity(horizontalVelocity, feetPosition, groundHit, deltaTime);
            }
            else
            {
                _rigidbody.linearVelocity = CalculateAirVelocity(horizontalVelocity, verticalSpeed, deltaTime);
            }
        }

        public void Rotate(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < MinSquaredDirectionLength)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _turnSpeed * deltaTime));
        }

        private void UpdateJumpState(float verticalSpeed)
        {
            if (_isJumping && verticalSpeed <= 0f)
            {
                _isJumping = false;
            }
        }

        private void UpdateGroundState(Vector3 feetPosition, out RaycastHit groundHit)
        {
            float searchDepth = _isGrounded ? _traversalSettings.StepHeight : GroundTolerance;
            bool hasGround = _groundSensor.TryFindGround(feetPosition, searchDepth, out groundHit);

            _isGrounded = hasGround && _isJumping == false;
        }

        private Vector3 CalculateGroundVelocity(Vector3 horizontalVelocity, Vector3 feetPosition, RaycastHit groundHit, float deltaTime)
        {
            if (ShouldJump(horizontalVelocity, feetPosition))
            {
                _isJumping = true;

                return new Vector3(horizontalVelocity.x, _traversalSettings.JumpSpeed, horizontalVelocity.z);
            }

            float slopeSpeed = CalculateSlopeSpeed(horizontalVelocity, groundHit.normal);
            float snapSpeed = (groundHit.point.y - feetPosition.y) * GroundSnapSmoothing / deltaTime;

            return new Vector3(horizontalVelocity.x, slopeSpeed + snapSpeed, horizontalVelocity.z);
        }

        private Vector3 CalculateAirVelocity(Vector3 horizontalVelocity, float verticalSpeed, float deltaTime)
        {
            float fallingSpeed = verticalSpeed - _traversalSettings.Gravity * deltaTime;

            return new Vector3(horizontalVelocity.x, fallingSpeed, horizontalVelocity.z);
        }

        private bool ShouldJump(Vector3 horizontalVelocity, Vector3 feetPosition)
        {
            if (horizontalVelocity.sqrMagnitude < MinSquaredDirectionLength)
            {
                return false;
            }

            return _jumpObstacleSensor.CanJumpOver(feetPosition, horizontalVelocity.normalized);
        }

        private float CalculateSlopeSpeed(Vector3 horizontalVelocity, Vector3 groundNormal)
        {
            return -(groundNormal.x * horizontalVelocity.x + groundNormal.z * horizontalVelocity.z) / groundNormal.y;
        }
    }
}
