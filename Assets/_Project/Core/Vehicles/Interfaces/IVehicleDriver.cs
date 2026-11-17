using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Contract implemented by entities capable of boarding and driving vehicles (e.g. AstronautController).
    /// </summary>
    public interface IVehicleDriver
    {
        GameObject DriverGameObject { get; }
        Transform DriverTransform { get; }

        void OnMountedToVehicle(IVehicle vehicle);
        void OnDismountedFromVehicle(Vector3 exitWorldPosition, Quaternion exitWorldRotation);
    }
}
