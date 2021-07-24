using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    [UsedImplicitly]
    public class ParticleAcceleratorPowerBoxSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ParticleAcceleratorPowerBoxComponent, PowerConsumerReceivedChanged>(
                PowerBoxReceivedChanged);
        }

        private static void PowerBoxReceivedChanged(
            EntityUid uid,
            ParticleAcceleratorPowerBoxComponent component,
            PowerConsumerReceivedChanged args)
        {
            component.Master!.PowerBoxReceivedChanged(args);
        }
    }
}
