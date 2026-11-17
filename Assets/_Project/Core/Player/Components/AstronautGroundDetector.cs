using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Executes physical ground and slope checks beneath the astronaut.
    /// Uses non-allocating sphere casting to guarantee zero garbage collection overhead.
    /// </summary>
    public class AstronautGroundDetector : MonoBehaviour, IGroundDetector
    {
        [Header("Ground Check Settings")]
        [Tooltip("Optional movement settings asset. If not provided, fallback values are used.")]
        [SerializeField] private AstronautMovementSettingsSO _settings;

        [Header("Custom Fallback Overrides")]
        [SerializeField] private float _defaultRadius = 0.28f;
        [SerializeField] private float _defaultOffset = 0.15f;
        [SerializeField] private LayerMask _defaultLayers = ~0;

        // Non-allocating physics buffer
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[4];

        private bool _isGrounded;
        private Vector3 _groundNormal = Vector3.up;
        private float _surfaceSlopeAngle;
        private float _groundDistance;
        private Vector3 _lastCheckOrigin;

        // IGroundDetector Implementation
        public bool IsGrounded => _isGrounded;
        public Vector3 GroundNormal => _groundNormal;
        public float SurfaceSlopeAngle => _surfaceSlopeAngle;
        public float GroundDistance => _groundDistance;

        public void Initialize(AstronautMovementSettingsSO settings)
        {
            _settings = settings;
        }

        public void CheckGround(Vector3 origin)
        {
            _lastCheckOrigin = origin;

            float radius = _settings != null ? _settings.GroundCheckRadius : _defaultRadius;
            float offset = _settings != null ? _settings.GroundCheckOffset : _defaultOffset;
            LayerMask layers = _settings != null ? _settings.GroundLayers : _defaultLayers;

            Vector3 castOrigin = origin + Vector3.up * (radius + offset);
            float castDistance = offset + 0.1f;

            // Zero-allocation physics query
            int hitCount = Physics.SphereCastNonAlloc(
                castOrigin,
                radius,
                Vector3.down,
                _hitBuffer,
                castDistance,
                layers,
                QueryTriggerInteraction.Ignore);

            if (hitCount > 0)
            {
                // Find closest hit that is not our own collider
                float closestDist = float.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < hitCount; i++)
                {
                    if (_hitBuffer[i].collider.transform.root == transform.root)
                        continue;

                    if (_hitBuffer[i].distance < closestDist)
                    {
                        closestDist = _hitBuffer[i].distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex != -1)
                {
                    _isGrounded = true;
                    _groundNormal = _hitBuffer[bestIndex].normal;
                    _surfaceSlopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
                    _groundDistance = _hitBuffer[bestIndex].distance;
                    return;
                }
            }

            // Airborne state
            _isGrounded = false;
            _groundNormal = Vector3.up;
            _surfaceSlopeAngle = 0f;
            _groundDistance = float.MaxValue;
        }

        private void OnDrawGizmosSelected()
        {
            float radius = _settings != null ? _settings.GroundCheckRadius : _defaultRadius;
            float offset = _settings != null ? _settings.GroundCheckOffset : _defaultOffset;

            Vector3 castOrigin = transform.position + Vector3.up * (radius + offset);
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(castOrigin, radius);
            Gizmos.DrawWireSphere(castOrigin + Vector3.down * (offset + 0.1f), radius);

            if (_isGrounded)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, _groundNormal * 1.5f);
            }
        }
    }
}
