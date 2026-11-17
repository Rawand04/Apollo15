using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Contract for providing astronaut locomotion input.
    /// Decouples physical input hardware (Unity Input System, AI agents, replays) from the motor.
    /// </summary>
    public interface IPlayerInputProvider
    {
        /// <summary>
        /// 2D input vector representing horizontal (X) and vertical (Y) stick or WASD movement.
        /// Normalized to a max magnitude of 1.
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// True if the sprint button is currently held down.
        /// </summary>
        bool IsSprintHeld { get; }

        /// <summary>
        /// True if non-zero movement input is currently registered.
        /// </summary>
        bool HasMoveInput { get; }

        /// <summary>
        /// Enables input processing.
        /// </summary>
        void EnableInput();

        /// <summary>
        /// Disables input processing and resets current input values.
        /// </summary>
        void DisableInput();
    }
}
