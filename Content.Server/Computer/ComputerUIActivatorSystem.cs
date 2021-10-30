using Content.Server.GameObjects.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Computer
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
