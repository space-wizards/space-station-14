using Content.Client.Storage;
using Content.Shared.Interaction;
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

            if (!TryComp<ClientStorageComponent>(container.Owner, out var storage))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            // return storage.UIOpen;

            // Need to check if UI is open
            return true;
        }
    }
}
