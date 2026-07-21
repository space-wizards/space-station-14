using Content.Server.Wires;
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
        if (!EntityManager.TryGetComponent(wire.Owner, out VendingMachineEjectComponent? eject))
            return StatusLightState.Off;

        return eject.CanShoot ? StatusLightState.BlinkingFast : StatusLightState.On;
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
        _vendingMachineSystem.EjectRandom(wire.Owner, true, vendComponent: vending);
    }
}
