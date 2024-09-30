using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Singularity.Components;
using Content.Shared.Wires;
using Robust.Shared.Player;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed partial class ParticleAcceleratorPowerWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-power";
    public override Color Color { get; set; } = Color.Yellow;
    public override object StatusKey { get; } = ParticleAcceleratorWireStatus.Power;

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        if (!component.CanBeEnabled)
            return StatusLightState.Off;
        return component.Enabled ? StatusLightState.On : StatusLightState.BlinkingSlow;
    }

    public override bool Cut(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();

        controller.CanBeEnabled = false;
        paSystem.SwitchOff(wire.Owner, user, controller);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.CanBeEnabled = true;
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();

        if (controller.Enabled)
            paSystem.SwitchOff(wire.Owner, user, controller);
        else if (controller.Assembled)
            paSystem.SwitchOn(wire.Owner, user, controller);
    }
}
