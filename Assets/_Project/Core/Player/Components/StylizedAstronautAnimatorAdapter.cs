using UnityEngine;

namespace SpaceSeries.Core.Player
{
    public sealed class StylizedAstronautAnimatorAdapter : MonoBehaviour, IAstronautAnimator
    {
        [SerializeField] private string _speedParameter = "Speed";
        private Animator _animator;
        private int _speedHash;
        private bool _hasSpeed;
        private bool _hasRiding;
        private bool _isRiding;
        private static readonly int RidingHash = Animator.StringToHash("IsRiding");
        private float _dampTime = 0.1f;
public void Initialize(Animator animator, AstronautMovementSettingsSO settings)
        { _dampTime = settings == null ? 0.1f : settings.AnimatorDampTime; SetAnimator(animator); }
        public void SetAnimator(Animator animator)
        {
            _animator = animator; _speedHash = Animator.StringToHash(_speedParameter); _hasSpeed = false; _hasRiding = false;
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == _speedHash && p.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                if (p.nameHash == RidingHash && p.type == AnimatorControllerParameterType.Bool) _hasRiding = true;
            }
        }
        public void SetRiding(bool riding)
        {
            _isRiding = riding;
            if (_animator == null) return;
            if (_hasSpeed) _animator.SetFloat(_speedHash, 0f);
            if (_hasRiding) _animator.SetBool(RidingHash, riding);
        }
        public void UpdateLocomotion(float currentPlanarSpeed, float targetSpeed, bool isSprinting, bool isGrounded, float verticalVelocity, float deltaTime)
        { if (_hasSpeed && _animator != null && _animator.isActiveAndEnabled) _animator.SetFloat(_speedHash, _isRiding ? 0f : currentPlanarSpeed, _dampTime, deltaTime); }
public void SetTrigger(int parameterHash)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters) if (p.nameHash == parameterHash && p.type == AnimatorControllerParameterType.Trigger) { _animator.SetTrigger(parameterHash); return; }
        }
        // This supplied rig has no gesture clips. Equipment placement uses its separate animation adapter.
        public void TriggerWave() { }
        public void TriggerPoint() { }
        public void TriggerPickUp() { }
    }
}
