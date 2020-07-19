#nullable enable
using Content.Shared.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class HandcuffComponent : Component, IInteractHand
    {
        public override string Name => "Handcuff";

        /// <summary>
        ///     The entity currently subdued by a <see cref="CuffedComponent"/>.
        /// </summary>
        public IEntity? CuffedEntity;

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables] public float cuffTime = 5.0f;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables] public float uncuffTime = 10.0f;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables] public float breakoutTime = 60.0f;

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            /*if (!CanPickup(eventArgs.User)) return false;

            var hands = eventArgs.User.GetComponent<Han>();
            hands.PutInHand(this, hands.ActiveIndex, fallback: false);
            return true;*/

            if (eventArgs.Target.TryGetComponent<SharedHandsComponent>(out var hands))
        }

    }
}
