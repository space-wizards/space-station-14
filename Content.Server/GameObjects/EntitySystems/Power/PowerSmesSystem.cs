#nullable enable
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class PowerSmesSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SmesComponent>(true))
            {
                comp.OnUpdate();
            }
        }
    }
}
