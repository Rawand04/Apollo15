namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Extensible modifier to scale or constrain astronaut locomotion speed.
    /// Can be implemented by ALSEP packages, terrain hazards, or suit life support modules.
    /// </summary>
    public interface ISpeedModifier
    {
        /// <summary>
        /// Speed multiplier (1.0 = normal, 0.5 = 50% speed penalty).
        /// </summary>
        float SpeedMultiplier { get; }

        /// <summary>
        /// Optional maximum speed clamp in m/s (e.g., carrying heavy ALSEP prevents sprinting).
        /// Set to float.MaxValue if uncapped.
        /// </summary>
        float MaxSpeedClamp { get; }

        /// <summary>
        /// Evaluation priority when multiple modifiers are active.
        /// Higher priorities apply first.
        /// </summary>
        int Priority { get; }
    }
}
