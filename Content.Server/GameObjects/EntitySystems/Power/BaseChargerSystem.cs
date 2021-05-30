#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BaseChargerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BaseCharger>(true))
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
