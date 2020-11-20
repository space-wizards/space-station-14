using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ConveyorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ConveyorComponent>())
            {
                comp.Update(frameTime);
            }
        }
    }
}
