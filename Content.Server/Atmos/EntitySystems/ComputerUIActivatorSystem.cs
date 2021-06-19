using Content.Shared.Interaction;
using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects;
using JetBrains.Annotations;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ComputerUIActivatorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BaseComputerUserInterfaceComponent, ActivateInWorldEvent>(HandleActivate);
        }

        private void HandleActivate(EntityUid uid, BaseComputerUserInterfaceComponent component, ActivateInWorldEvent args)
        {
            component.ActivateThunk(args);
        }
    }
}
