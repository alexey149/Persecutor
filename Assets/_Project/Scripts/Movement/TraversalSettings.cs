using UnityEngine;

namespace Persecutor.Movement
{
    [CreateAssetMenu(fileName = nameof(TraversalSettings), menuName = "Persecutor/Traversal Settings")]
    public class TraversalSettings : ScriptableObject
    {
        private const float MaxSlopeLimit = 89f;
        private const float JumpSpeedFactor = 2f;

        [SerializeField, Min(0f)] private float _stepHeight = 0.45f;
        [SerializeField, Range(0f, MaxSlopeLimit)] private float _slopeLimit = 45f;
        [SerializeField, Min(0f)] private float _jumpHeight = 1.2f;
        [SerializeField, Min(0f)] private float _gravity = 20f;

        private float _minGroundNormalY;
        private float _jumpSpeed;

        public float StepHeight => _stepHeight;
        public float SlopeLimit => _slopeLimit;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;
        public float MinGroundNormalY => _minGroundNormalY;
        public float JumpSpeed => _jumpSpeed;

        private void OnEnable()
        {
            CalculateDerivedValues();
        }

        private void OnValidate()
        {
            CalculateDerivedValues();
        }

        private void CalculateDerivedValues()
        {
            _minGroundNormalY = Mathf.Cos(_slopeLimit * Mathf.Deg2Rad);
            _jumpSpeed = Mathf.Sqrt(JumpSpeedFactor * _gravity * _jumpHeight);
        }
    }
}
