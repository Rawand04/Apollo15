using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Contract for surface detection and ground normal evaluation.
    /// Crucial for low-gravity terrain alignment and slope sliding calculations.
    /// </summary>
    public interface IGroundDetector
    {
        /// <summary>
        /// True if the astronaut is contacting the ground surface.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Normal vector of the detected surface beneath the astronaut.
        /// </summary>
        Vector3 GroundNormal { get; }

        /// <summary>
        /// Slope angle in degrees of the surface beneath the astronaut.
        /// </summary>
        float SurfaceSlopeAngle { get; }

        /// <summary>
        /// Distance from the ground probe origin to the detected surface point.
        /// </summary>
        float GroundDistance { get; }

        /// <summary>
        /// Executes non-allocating physical ground detection check.
        /// </summary>
        void CheckGround(Vector3 origin);
    }
}
