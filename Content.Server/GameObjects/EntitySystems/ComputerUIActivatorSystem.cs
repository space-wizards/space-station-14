using System.Collections.Generic;
using System.Linq;
using Content.Shared.Interaction;
using Content.Server.GameObjects.Components;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
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
