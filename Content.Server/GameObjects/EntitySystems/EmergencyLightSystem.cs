using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class EmergencyLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<EmergencyLightComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
