using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    public sealed partial class ParticleAcceleratorSystem
    {
        private void InitializePowerBoxSystem()
        {
            SubscribeLocalEvent<ParticleAcceleratorPowerBoxComponent, PowerConsumerReceivedChanged>(PowerBoxReceivedChanged);
        }

        private void PowerBoxReceivedChanged(
            EntityUid uid,
            ParticleAcceleratorPowerBoxComponent component,
            ref PowerConsumerReceivedChanged args)
        {
            if (TryComp(uid, out ParticleAcceleratorPartComponent? paPart))
                paPart.Master?.PowerBoxReceivedChanged(args);
        }
    }
}
