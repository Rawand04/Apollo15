using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Contract implemented by any interactable object on the lunar/planetary surface
    /// (e.g. Falcon LM ladder, ALSEP experiments, Lunar Roving Vehicle seat, rock samples).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Object title or description shown in the HUD prompt (e.g., 'Passive Seismic Experiment').
        /// </summary>
        string PromptText { get; }

        /// <summary>
        /// Action verb describing what 'E' will execute (e.g., 'Deploy', 'Collect', 'Inspect', 'Board').
        /// </summary>
        string ActionVerb { get; }

        /// <summary>
        /// Whether the object is currently capable of being interacted with.
        /// </summary>
        bool IsInteractable { get; }

        /// <summary>
        /// World-space center point used for distance and line-of-sight checks.
        /// </summary>
        Vector3 InteractionPosition { get; }

        /// <summary>
        /// Verifies whether the specific interactor satisfies preconditions (e.g., hands free, in range).
        /// </summary>
        bool CanInteract(IInteractor interactor);

        /// <summary>
        /// Invoked when the player focuses their gaze/crosshair onto this object.
        /// </summary>
        void OnFocusGained(IInteractor interactor);

        /// <summary>
        /// Invoked when the player looks away or steps out of interaction range.
        /// </summary>
        void OnFocusLost(IInteractor interactor);

        /// <summary>
        /// Executes the interaction logic when 'E' is pressed.
        /// Returns an InteractionResult with dialogue, animation, and quest payloads.
        /// </summary>
        InteractionResult Interact(IInteractor interactor);
    }
}
