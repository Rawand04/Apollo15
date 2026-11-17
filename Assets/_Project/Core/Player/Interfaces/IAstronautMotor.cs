using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Contract for kinematic locomotion, rotation smoothing, and planetary gravity integration.
    /// </summary>
    public interface IAstronautMotor
    {
        /// <summary>
        /// Total calculated world-space velocity of the astronaut.
        /// </summary>
        Vector3 Velocity { get; }

        /// <summary>
        /// Current planar (horizontal) speed in m/s.
        /// </summary>
        float CurrentPlanarSpeed { get; }

        /// <summary>
        /// Current vertical velocity (affected by Moon gravity and grounding).
        /// </summary>
        float VerticalVelocity { get; }

        /// <summary>
        /// Initializes the motor with settings and component references.
        /// </summary>
        void Initialize(
            AstronautMovementSettingsSO settings,
            CharacterController characterController,
            IGroundDetector groundDetector,
            Transform astronautTransform,
            Transform cameraTransform);

        /// <summary>
        /// Updates locomotion physics for the current frame.
        /// </summary>
        void Tick(Vector2 inputDirection, bool isSprintRequested, float deltaTime);

        /// <summary>
        /// Registers a dynamic speed modifier (e.g., equipment weight, terrain slope).
        /// </summary>
        void AddSpeedModifier(ISpeedModifier modifier);

        /// <summary>
        /// Removes an existing speed modifier.
        /// </summary>
        void RemoveSpeedModifier(ISpeedModifier modifier);

        /// <summary>
        /// Teleports astronaut to a new position, clearing residual momentum.
        /// </summary>
        void Teleport(Vector3 worldPosition, Quaternion worldRotation);

        /// <summary>
        /// Halts all horizontal and vertical velocity.
        /// </summary>
        void ResetVelocity();
    }
}
