using Persecutor.Movement;
using Persecutor.Player;
using UnityEngine;

namespace Persecutor.Bot
{
    [RequireComponent(typeof(BotMover))]
    public class BotChaser : MonoBehaviour
    {
        private const float MinSpeedRatio = 0.1f;
        private const float MaxSpeedRatio = 0.95f;

        [SerializeField] private PlayerMovement _player;
        [SerializeField, Range(MinSpeedRatio, MaxSpeedRatio)] private float _speedRatio = 0.7f;
        [SerializeField, Min(0f)] private float _stopDistance = 3f;
        [SerializeField, Min(0f)] private float _resumeDistance = 4f;
        [SerializeField, Min(0f)] private float _trailPointSpacing = 0.5f;
        [SerializeField, Min(0f)] private float _trailPointReachRadius = 0.5f;

        private BotMover _mover;
        private PlayerTrail _trail;
        private float _squaredStopDistance;
        private float _squaredResumeDistance;
        private bool _isChasing = true;

        private float Speed => _player.MoveSpeed * _speedRatio;
        private Vector3 PlayerPosition => _player.transform.position;

        private void Awake()
        {
            _mover = GetComponent<BotMover>();
            _trail = new PlayerTrail(_trailPointSpacing, _trailPointReachRadius);
            _squaredStopDistance = _stopDistance * _stopDistance;
            _squaredResumeDistance = _resumeDistance * _resumeDistance;
        }

        private void OnValidate()
        {
            _resumeDistance = Mathf.Max(_resumeDistance, _stopDistance);
        }

        private void FixedUpdate()
        {
            UpdateChaseState();
            UpdateTrail();

            Vector3 horizontalVelocity = CalculateHorizontalVelocity();

            _mover.Move(horizontalVelocity, Time.fixedDeltaTime);
            _mover.Rotate(CalculateLookDirection(horizontalVelocity), Time.fixedDeltaTime);
        }

        private void UpdateChaseState()
        {
            float squaredDistance = (PlayerPosition - _mover.Position).sqrMagnitude;

            if (squaredDistance <= _squaredStopDistance)
            {
                _isChasing = false;
            }
            else if (squaredDistance >= _squaredResumeDistance)
            {
                _isChasing = true;
            }
        }

        private void UpdateTrail()
        {
            if (_isChasing)
            {
                _trail.Record(PlayerPosition);
            }
            else
            {
                _trail.Clear();
            }
        }

        private Vector3 CalculateHorizontalVelocity()
        {
            if (_isChasing == false)
            {
                return Vector3.zero;
            }

            Vector3 position = _mover.Position;
            Vector3 target = _trail.TryGetNextPoint(position, out Vector3 trailPoint) ? trailPoint : PlayerPosition;

            return (target - position).Flatten().normalized * Speed;
        }

        private Vector3 CalculateLookDirection(Vector3 horizontalVelocity)
        {
            if (_isChasing)
            {
                return horizontalVelocity;
            }

            return (PlayerPosition - _mover.Position).Flatten();
        }
    }
}
