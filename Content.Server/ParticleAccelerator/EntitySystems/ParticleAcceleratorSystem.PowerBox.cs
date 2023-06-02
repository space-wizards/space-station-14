using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.ParticleAccelerator.EntitySystems
{
    public sealed partial class ParticleAcceleratorSystem
    {
        private void InitializePowerBoxSystem()
        {
            SubscribeLocalEvent<ParticleAcceleratorPowerBoxComponent, PowerConsumerReceivedChanged>(PowerBoxReceivedChanged);
        }

        private void PowerBoxReceivedChanged(EntityUid uid, ParticleAcceleratorPowerBoxComponent component, ref PowerConsumerReceivedChanged args)
        {
            if (!TryComp<ParticleAcceleratorPartComponent>(uid, out var part))
                return;
            if (!TryComp<ParticleAcceleratorControlBoxComponent>(part.Master, out var controller))
                return;
            if (!controller.Enabled)
                return;

            DebugTools.Assert(controller.Assembled);

            var master = part.Master!.Value;
            if (args.ReceivedPower >= args.DrawRate)
                PowerOn(master, comp: controller);
            else
                PowerOff(master, comp: controller);

            UpdateUI(master, controller);
        }
    }
}
