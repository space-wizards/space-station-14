using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.EntitySystems
{
    [UsedImplicitly]
    public class SharedStorageSystem : EntitySystem
    {
        protected void OnOpenCloseEvent(OpenCloseBagEvent args)
        {
            if (!ComponentManager.TryGetComponent<SharedAppearanceComponent>(args.Owner, out var appearanceComponent))
                return;

            appearanceComponent.SetData(SharedBagOpenVisuals.BagState, args.State);
            if (ComponentManager.HasComponent<ItemCounterComponent>(args.Owner))
            {
                appearanceComponent.SetData(StackVisuals.Hide, args.IsClosed);
            }
        }
    }
}
