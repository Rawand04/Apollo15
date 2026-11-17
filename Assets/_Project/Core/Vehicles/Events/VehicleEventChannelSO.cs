using System;
using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Event Channel ScriptableObject broadcasting vehicle state changes.
    /// Drives HUD telemetry (speedometer, battery reserve, compass heading) and camera transitions.
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleEventChannel", menuName = "SpaceSeries/Vehicles/Vehicle Event Channel")]
    public class VehicleEventChannelSO : ScriptableObject
    {
        public event Action<IVehicle> OnVehicleBoarded;
        public event Action<IVehicle> OnVehicleDismounted;
        public event Action<float> OnSpeedChanged; // Speed in km/h

        public void RaiseVehicleBoarded(IVehicle vehicle)
        {
            OnVehicleBoarded?.Invoke(vehicle);
        }

        public void RaiseVehicleDismounted(IVehicle vehicle)
        {
            OnVehicleDismounted?.Invoke(vehicle);
        }

        public void RaiseSpeedChanged(float speedKmh)
        {
            OnSpeedChanged?.Invoke(speedKmh);
        }
    }
}
