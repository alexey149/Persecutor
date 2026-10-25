using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor.Joints
{
    public class Catapult : MonoBehaviour
    {
        [SerializeField] private InteractionRange _interactionRange;
        [SerializeField] private Rigidbody _spoon;
        [SerializeField] private SpringJoint _spring;
        [SerializeField] private Transform _restAnchor;
        [SerializeField] private Transform _launchAnchor;
        [SerializeField] private Transform _projectileSpawnPoint;
        [SerializeField] private Rigidbody _projectilePrefab;
        [SerializeField] private InputActionReference _launchAction;
        [SerializeField] private InputActionReference _reloadAction;
        [SerializeField, Min(0f)] private float _restSpring = 50f;
        [SerializeField, Min(0f)] private float _launchSpring = 15f;
        [SerializeField, Min(0f)] private float _restAngleTolerance = 2f;
        [SerializeField, Min(0f)] private float _restAngularSpeedTolerance = 0.1f;

        private Quaternion _restRotation;
        private bool _isLoaded;
        private bool _isReloading;

        private void Awake()
        {
            _restRotation = _spoon.rotation;
        }

        private void OnEnable()
        {
            _launchAction.action.performed += OnLaunch;
            _reloadAction.action.performed += OnReload;
            _launchAction.action.Enable();
            _reloadAction.action.Enable();
        }

        private void OnDisable()
        {
            _launchAction.action.performed -= OnLaunch;
            _reloadAction.action.performed -= OnReload;
            _launchAction.action.Disable();
            _reloadAction.action.Disable();
        }

        private void Start()
        {
            PullSpoonTo(_restAnchor, _restSpring);
            SpawnProjectile();
        }

        private void OnLaunch(InputAction.CallbackContext context)
        {
            if (_isLoaded == false || _interactionRange.IsPlayerInRange() == false)
            {
                return;
            }

            _isLoaded = false;
            PullSpoonTo(_launchAnchor, _launchSpring);
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if (_isLoaded || _isReloading || _interactionRange.IsPlayerInRange() == false)
            {
                return;
            }

            StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            _isReloading = true;
            PullSpoonTo(_restAnchor, _restSpring);

            yield return new WaitUntil(IsSpoonAtRest);

            SpawnProjectile();
            _isReloading = false;
        }

        private bool IsSpoonAtRest()
        {
            float angleFromRest = Quaternion.Angle(_spoon.rotation, _restRotation);
            float squaredAngularSpeedTolerance = _restAngularSpeedTolerance * _restAngularSpeedTolerance;
            bool isAtRestAngle = angleFromRest <= _restAngleTolerance;
            bool isStopped = _spoon.angularVelocity.sqrMagnitude <= squaredAngularSpeedTolerance;

            return isAtRestAngle && isStopped;
        }

        private void PullSpoonTo(Transform anchor, float spring)
        {
            _spring.connectedAnchor = anchor.position;
            _spring.spring = spring;
            _spoon.WakeUp();
        }

        private void SpawnProjectile()
        {
            Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
            _isLoaded = true;
        }
    }
}
