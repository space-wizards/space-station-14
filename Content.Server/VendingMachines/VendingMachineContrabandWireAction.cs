using Content.Server.Wires;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;

namespace Content.Server.VendingMachines;

[DataDefinition]
public sealed partial class VendingMachineContrabandWireAction : ComponentWireAction<VendingMachineComponent>
{
    private VendingMachineSystem _vendingMachineSystem = default!;

    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-vending-contraband";
    public override object? StatusKey { get; } = ContrabandWireKey.StatusKey;
    // TODO probably gonna need this for the pulse action? delete otherwise
    //public override object? TimeoutKey { get; } = ContrabandWireKey.TimeoutKey;

    public override void Initialize()
    {
        base.Initialize();

        _vendingMachineSystem = EntityManager.System<VendingMachineSystem>();
    }

    public override StatusLightState? GetLightState(Wire wire, VendingMachineComponent component)
        => component.Contraband ? StatusLightState.BlinkingSlow : StatusLightState.On;

    public override bool Cut(EntityUid user, Wire wire, VendingMachineComponent component)
    {
        _vendingMachineSystem.SetContraband(wire.Owner, true, component);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, VendingMachineComponent component)
    {
        _vendingMachineSystem.SetContraband(wire.Owner, false, component);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, VendingMachineComponent component)
    {
        throw new NotImplementedException(); // TODO
    }
}
