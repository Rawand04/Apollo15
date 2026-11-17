using System;
using UnityEngine;

namespace SpaceSeries.Core.Vehicles
{
    /// <summary>
    /// Synchronizes a visual 3D wheel mesh transform with a physical WheelCollider.
    /// Accurately reflects suspension compression, wheel rotation, and steering angle.
    /// </summary>
    [Serializable]
    public class RoverWheelPair
    {
        [Tooltip("The physical WheelCollider attached to the rover chassis.")]
        public WheelCollider PhysicalCollider;

        [Tooltip("The visual 3D wheel mesh transform rendered in the scene.")]
        public Transform VisualMesh;

        public void UpdatePose()
        {
            if (PhysicalCollider == null || VisualMesh == null)
                return;

            PhysicalCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            VisualMesh.SetPositionAndRotation(position, rotation);
        }
    }
}
