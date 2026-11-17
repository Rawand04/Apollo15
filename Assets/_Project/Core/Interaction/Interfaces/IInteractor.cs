using UnityEngine;

namespace SpaceSeries.Core.Interaction
{
    /// <summary>
    /// Contract for the entity initiating interactions (the player astronaut).
    /// </summary>
    public interface IInteractor
    {
        /// <summary>
        /// Transform of the interactor (used for distance and orientation checks).
        /// </summary>
        Transform InteractorTransform { get; }

        /// <summary>
        /// Root GameObject of the interactor.
        /// </summary>
        GameObject InteractorGameObject { get; }

        /// <summary>
        /// The currently focused interactable target, or null if none.
        /// </summary>
        IInteractable CurrentTarget { get; }

        /// <summary>
        /// Triggers an interaction animation on the astronaut (e.g., 'Interact_Reach', 'Interact_Deploy').
        /// </summary>
        void TriggerInteractionAnimation(string triggerParamName);
    }
}
