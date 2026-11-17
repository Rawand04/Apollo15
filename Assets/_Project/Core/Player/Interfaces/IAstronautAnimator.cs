using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Contract for driving astronaut animation blend trees and states.
    /// </summary>
    public interface IAstronautAnimator
    {
        /// <summary>
        /// Initializes parameter hashes and cached references.
        /// </summary>
        void Initialize(Animator animator, AstronautMovementSettingsSO settings);

        /// <summary>
        /// Feeds locomotion and physics state to the Animator with parameter dampening.
        /// </summary>
        void UpdateLocomotion(
            float currentPlanarSpeed,
            float targetSpeed,
            bool isSprinting,
            bool isGrounded,
            float verticalVelocity,
            float deltaTime);

        /// <summary>
        /// Sets a trigger parameter on the Animator safely.
        /// </summary>
        void SetTrigger(int parameterHash);

        /// <summary>
        /// Triggers the wave gesture animation.
        /// </summary>
        void TriggerWave();

        /// <summary>
        /// Triggers the pointing gesture animation.
        /// </summary>
        void TriggerPoint();

        /// <summary>
        /// Triggers the pickup / interaction animation.
        /// </summary>
        void TriggerPickUp();

        /// <summary>
        /// Updates the target Animator reference (e.g. if avatar is dynamically swapped).
        /// </summary>
        void SetAnimator(Animator animator);
    }
}
