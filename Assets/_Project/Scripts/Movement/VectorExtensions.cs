using UnityEngine;

namespace Persecutor.Movement
{
    public static class VectorExtensions
    {
        public static Vector3 Flatten(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
