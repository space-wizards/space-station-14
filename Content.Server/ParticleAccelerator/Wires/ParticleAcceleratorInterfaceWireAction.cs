using Content.Server.ParticleAccelerator.Components;
using Content.Server.Wires;
using Content.Shared.Wires;

namespace Content.Server.ParticleAccelerator.Wires;

public sealed class ParticleAcceleratorInterfaceWireAction : ComponentWireAction<ParticleAcceleratorControlBoxComponent>
{
    public override string Name { get; set; } = "wire-name-pa-interface";
    public override Color Color { get; set; } = Color.LimeGreen;
    public override object StatusKey { get; } = ParticleAcceleratorControlBoxWires.Interface;

    public override StatusLightState? GetLightState(Wire wire, ParticleAcceleratorControlBoxComponent component)
    {
        return component.InterfaceDisabled ? StatusLightState.BlinkingFast : StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = true;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = false;
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ParticleAcceleratorControlBoxComponent controller)
    {
        controller.InterfaceDisabled = !controller.InterfaceDisabled;
    }
}
