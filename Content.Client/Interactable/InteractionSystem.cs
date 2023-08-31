using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Client.Interactable
{
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (!EntityManager.EntityExists(target))
                return false;

            if (!target.TryGetContainer(out var container))
                return false;

            if (!TryComp(container.Owner, out StorageComponent? storage))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            // Need to return if UI is open or not
            return true;
        }
    }
}
