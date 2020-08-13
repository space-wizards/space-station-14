using Content.Server.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class TemperatureSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<TemperatureComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
