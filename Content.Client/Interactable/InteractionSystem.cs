using Content.Client.Storage;
using Content.Shared.Interaction;

namespace Content.Client.Interactable
{
    public sealed class InteractionSystem : SharedInteractionSystem
    {
        public override bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (!EntityManager.EntityExists(target))
                return false;

            if (!ContainerSystem.TryGetContainingContainer(target, out var container))
                return false;

            if (!HasComp<ClientStorageComponent>(container.Owner))
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            // Need to return if UI is open or not
            return true;
        }
    }
}
