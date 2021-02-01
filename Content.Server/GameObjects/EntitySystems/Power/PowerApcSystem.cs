#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using JetBrains.Annotations;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PowerApcSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var apc in ComponentManager.EntityQuery<ApcComponent>(false))
            {
                apc.Update();
            }
        }
    }
}
