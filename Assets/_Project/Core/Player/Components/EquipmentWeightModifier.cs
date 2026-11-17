using UnityEngine;

namespace SpaceSeries.Core.Player
{
    /// <summary>
    /// Example speed modifier demonstrating the Open/Closed Principle.
    /// Can be attached to equipment (e.g. ALSEP Central Station, Drill Core, Sample Boxes)
    /// to adjust astronaut locomotion speed when carried without modifying controller code.
    /// </summary>
    public class EquipmentWeightModifier : MonoBehaviour, ISpeedModifier
    {
        [Tooltip("Speed multiplier applied to the astronaut (e.g. 0.65 = 35% speed penalty).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _speedMultiplier = 0.65f;

        [Tooltip("Maximum allowed speed in m/s while carrying this equipment (e.g., prevents sprinting).")]
        [SerializeField] private float _maxSpeedClamp = 2.0f;

        [Tooltip("Priority order when multiple modifiers are active.")]
        [SerializeField] private int _priority = 10;

        public float SpeedMultiplier => _speedMultiplier;
        public float MaxSpeedClamp => _maxSpeedClamp;
        public int Priority => _priority;
    }
}
