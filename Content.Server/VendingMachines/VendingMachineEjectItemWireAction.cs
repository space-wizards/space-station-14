using Content.Server.Wires;
using Content.Server.VendingMachines.Components;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Wires;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineEjectItemWireAction : ComponentWireAction<VendingMachineComponent>
{
    private VendingMachineSystem _vendingMachineSystem = default!;

    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-vending-eject";

    public override object StatusKey => EjectWireKey.StatusKey;

    public override StatusLightState? GetLightState(Wire wire, VendingMachineComponent comp)
    {
        if (!EntityManager.HasComponent<VendingMachineEjectComponent>(wire.Owner))
            return StatusLightState.Off;

        return EntityManager.HasComponent<VendingMachineShootComponent>(wire.Owner)
            ? StatusLightState.BlinkingFast
            : StatusLightState.On;
    }

    public override void Initialize()
    {
        base.Initialize();

        _vendingMachineSystem = EntityManager.System<VendingMachineSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.SetShooting(wire.Owner, true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.SetShooting(wire.Owner, false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, VendingMachineComponent vending)
    {
        _vendingMachineSystem.EjectRandom((wire.Owner, vending), true);
    }
}
