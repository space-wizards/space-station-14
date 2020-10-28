using Content.Server.GameObjects.Components.Instruments;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class InstrumentSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<InstrumentComponent>())
            {
                component.Update(frameTime);
            }
        }
    }
}
