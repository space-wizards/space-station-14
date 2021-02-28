using Content.Server.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ConveyorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ConveyorComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
