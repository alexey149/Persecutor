using System.Collections.Generic;
using Persecutor.Movement;
using UnityEngine;

namespace Persecutor.Bot
{
    public class PlayerTrail
    {
        private readonly Queue<Vector3> _points = new Queue<Vector3>();
        private readonly float _squaredPointSpacing;
        private readonly float _squaredReachRadius;

        private Vector3 _lastPoint;

        public PlayerTrail(float pointSpacing, float reachRadius)
        {
            _squaredPointSpacing = pointSpacing * pointSpacing;
            _squaredReachRadius = reachRadius * reachRadius;
        }

        public void Record(Vector3 point)
        {
            if (_points.Count > 0 && (point - _lastPoint).sqrMagnitude < _squaredPointSpacing)
            {
                return;
            }

            _points.Enqueue(point);
            _lastPoint = point;
        }

        public void Clear()
        {
            _points.Clear();
        }

        public bool TryGetNextPoint(Vector3 position, out Vector3 point)
        {
            while (_points.Count > 0 && IsReached(_points.Peek(), position))
            {
                _points.Dequeue();
            }

            return _points.TryPeek(out point);
        }

        private bool IsReached(Vector3 point, Vector3 position)
        {
            return (point - position).Flatten().sqrMagnitude < _squaredReachRadius;
        }
    }
}
