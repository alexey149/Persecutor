using UnityEngine;

namespace Persecutor.Joints
{
    public class InteractionRange : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private Collider[] _colliders;
        [SerializeField, Min(0f)] private float _radius = 1.5f;

        public bool IsPlayerInRange()
        {
            Vector3 playerPosition = _player.position;
            float squaredRadius = _radius * _radius;

            foreach (Collider objectCollider in _colliders)
            {
                Vector3 closestPoint = objectCollider.ClosestPoint(playerPosition);

                if ((closestPoint - playerPosition).sqrMagnitude <= squaredRadius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
