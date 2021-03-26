using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Client.GameObjects.EntitySystems
{
    internal sealed class HandsSystem : SharedHandsSystem
    {
        protected override void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out HandsComponent? hands))
            {
                hands.UpdateHandsSet();
                hands.UpdateHandVisualizer();
                hands.UpdateHandsGuiState();
            }
        }
    }
}
