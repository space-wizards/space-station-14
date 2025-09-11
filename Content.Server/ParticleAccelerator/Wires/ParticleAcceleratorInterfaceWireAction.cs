using Content.Server.ParticleAccelerator.Components;
using Content.Server.ParticleAccelerator.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Singularity.Components;
using Content.Shared.Wires;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed partial class ParticleAcceleratorKeyboardWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-keyboard";
    public override Color Color { get; set; } = Color.LimeGreen;
    public override object StatusKey { get; } = ParticleAcceleratorWireStatus.Keyboard;

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        return component.InterfaceDisabled ? StatusLightState.BlinkingFast : StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = true;
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        paSystem.UpdateUI(wire.Owner, controller);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = false;
        var paSystem = EntityManager.System<ParticleAcceleratorSystem>();
        paSystem.UpdateUI(wire.Owner, controller);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = !controller.InterfaceDisabled;
    }
}
