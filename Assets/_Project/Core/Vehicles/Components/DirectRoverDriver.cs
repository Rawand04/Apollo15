using UnityEngine;
namespace SpaceSeries.Core.Vehicles
{
    [RequireComponent(typeof(RoverController))]
    public sealed class DirectRoverDriver : MonoBehaviour, IVehicleDriver
    {
        private RoverController _rover;
        private Vector2 _move;
        private bool _brake;
        public bool IsDriving { get; private set; }
        public GameObject DriverGameObject => gameObject;
        public Transform DriverTransform => transform;
        private void Awake() { _rover = GetComponent<RoverController>(); }
        public bool BeginDriving()
        {
            if (_rover == null) _rover = GetComponent<RoverController>();
            if (IsDriving) return true;
            if (!_rover.isActiveAndEnabled || !_rover.Board(this)) return false;
            IsDriving = true; _move = Vector2.zero; _brake = false;
            return true;
        }
        public void StopDriving()
        {
            _move = Vector2.zero; _brake = false;
            if (IsDriving && _rover != null) _rover.Dismount();
            IsDriving = false;
        }
        public void SetMove(Vector2 value) { _move = Vector2.ClampMagnitude(value, 1f); }
        public void SetBrake(bool value) { _brake = value; }
        private void Update()
        {
            if (!IsDriving || _rover == null) return;
            if (!_rover.isActiveAndEnabled || !_rover.IsOccupied) { StopDriving(); return; }
            _rover.SetDriveInput(_move, _brake);
        }
        private void OnDisable() { StopDriving(); }
        public void OnMountedToVehicle(IVehicle vehicle) { }
        public void OnDismountedFromVehicle(Vector3 exitWorldPosition, Quaternion exitWorldRotation) { StopDriving(); }
    }
}
