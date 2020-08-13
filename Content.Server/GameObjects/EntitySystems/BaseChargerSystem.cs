using Content.Server.GameObjects.Components.Power.Chargers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class BaseChargerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BaseCharger>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
