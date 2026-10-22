using Persecutor.Movement;
using UnityEngine;

namespace Persecutor.Bot
{
    public class JumpObstacleSensor
    {
        private const float RayHeightAboveStep = 0.05f;

        private readonly TraversalSettings _traversalSettings;
        private readonly LayerMask _obstacleMask;
        private readonly float _checkDistance;
        private readonly float _clearanceCheckDistance;

        public JumpObstacleSensor(TraversalSettings traversalSettings, LayerMask obstacleMask, float checkDistance, float clearanceCheckDistance)
        {
            _traversalSettings = traversalSettings;
            _obstacleMask = obstacleMask;
            _checkDistance = checkDistance;
            _clearanceCheckDistance = clearanceCheckDistance;
        }

        public bool CanJumpOver(Vector3 feetPosition, Vector3 direction)
        {
            return IsWallAboveStep(feetPosition, direction) && IsClearAtJumpHeight(feetPosition, direction);
        }

        private bool IsWallAboveStep(Vector3 feetPosition, Vector3 direction)
        {
            Vector3 origin = feetPosition + Vector3.up * (_traversalSettings.StepHeight + RayHeightAboveStep);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, _checkDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return false;
            }

            return hit.normal.y < _traversalSettings.MinGroundNormalY;
        }

        private bool IsClearAtJumpHeight(Vector3 feetPosition, Vector3 direction)
        {
            Vector3 origin = feetPosition + Vector3.up * _traversalSettings.JumpHeight;

            return Physics.Raycast(origin, direction, _clearanceCheckDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false;
        }
    }
}
