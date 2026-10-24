using Persecutor.Movement;
using UnityEngine;

namespace Persecutor.Animations
{
    public class CharacterAnimation : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int IsGroundedParameter = Animator.StringToHash("IsGrounded");

        [SerializeField] private Animator _animator;

        public void SetMovement(Vector3 velocity, bool isGrounded)
        {
            _animator.SetFloat(SpeedParameter, velocity.Flatten().magnitude);
            _animator.SetBool(IsGroundedParameter, isGrounded);
        }
    }
}
