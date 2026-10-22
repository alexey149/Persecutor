using Persecutor.Movement;
using UnityEngine;

namespace Persecutor.Bot
{
    public class GroundSensor
    {
        private const float RayStartOffset = 0.05f;

        private readonly TraversalSettings _traversalSettings;
        private readonly LayerMask _groundMask;

        public GroundSensor(TraversalSettings traversalSettings, LayerMask groundMask)
        {
            _traversalSettings = traversalSettings;
            _groundMask = groundMask;
        }

        public bool TryFindGround(Vector3 feetPosition, float searchDepth, out RaycastHit groundHit)
        {
            float rayStartHeight = _traversalSettings.StepHeight + RayStartOffset;
            Vector3 origin = feetPosition + Vector3.up * rayStartHeight;
            float rayLength = rayStartHeight + searchDepth;

            if (Physics.Raycast(origin, Vector3.down, out groundHit, rayLength, _groundMask, QueryTriggerInteraction.Ignore) == false)
            {
                return false;
            }

            return groundHit.normal.y >= _traversalSettings.MinGroundNormalY;
        }
    }
}
