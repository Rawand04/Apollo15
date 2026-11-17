using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Contract for driveable vehicles (Apollo LRV, Martian rovers, habitat tractors).
    /// </summary>
    public interface IVehicle
    {
        bool IsOccupied { get; }
        float CurrentSpeedMs { get; }
        float CurrentSpeedKmh { get; }
        Rigidbody VehicleRigidbody { get; }

        bool Board(IVehicleDriver driver);
        void Dismount();
        void SetDriveInput(Vector2 moveInput, bool isBraking);
        void ApplyHandbrake();
        void ReleaseHandbrake();
    }
}
