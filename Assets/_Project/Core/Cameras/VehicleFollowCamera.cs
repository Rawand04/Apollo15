using UnityEngine;
namespace SpaceSeries.Core.Cameras
{
    public sealed class VehicleFollowCamera : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _smoothingTime = 0.15f;
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [SerializeField] private float _stickSensitivity = 90f;
        [SerializeField] private LayerMask _collisionMask = ~0;
        private Transform _target;
        private Transform _ignoredRoot;
        private float _distance = 6f;
        private float _yaw;
        private float _pitch = 25f;
        private Vector2 _stickLook;
        private Vector2 _mouseDelta;
        private Vector3 _velocity;
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        public Transform Target => _target;
        public void SelectTarget(Transform target, Transform ignoredRoot, float distance)
        {
            _target = target; _ignoredRoot = ignoredRoot; _distance = distance;
            _yaw = target == null ? 0f : target.eulerAngles.y;
            _pitch = 25f; _velocity = Vector3.zero; ClearLook();
        }
        public void SetLook(Vector2 value, bool pointer)
        {
            if (pointer) _mouseDelta = value; else _stickLook = value;
        }
        public void ClearLook() { _stickLook = Vector2.zero; _mouseDelta = Vector2.zero; }
        private void LateUpdate()
        {
            if (_target == null) return;
            Vector2 look = _mouseDelta * _mouseSensitivity + _stickLook * (_stickSensitivity * Time.deltaTime);
            _mouseDelta = Vector2.zero;
            _yaw += look.x; _pitch = Mathf.Clamp(_pitch - look.y, 15f, 70f);
            Vector3 pivot = _target.position;
            Vector3 direction = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.back;
            float distance = _distance;
            int count = Physics.SphereCastNonAlloc(pivot, 0.2f, direction, _hits, _distance, _collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (_ignoredRoot == null || !_hits[i].transform.IsChildOf(_ignoredRoot))
                    distance = Mathf.Min(distance, Mathf.Max(0.5f, _hits[i].distance - 0.25f));
            Vector3 destination = pivot + direction * distance;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, _smoothingTime);
            // Clamp smoothed travel too, preventing interpolation through terrain.
            Vector3 travel = transform.position - pivot;
            count = Physics.SphereCastNonAlloc(pivot, 0.15f, travel.normalized, _hits, travel.magnitude, _collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (_ignoredRoot == null || !_hits[i].transform.IsChildOf(_ignoredRoot))
                    transform.position = pivot + travel.normalized * Mathf.Min((transform.position - pivot).magnitude, Mathf.Max(0.5f, _hits[i].distance - 0.2f));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
