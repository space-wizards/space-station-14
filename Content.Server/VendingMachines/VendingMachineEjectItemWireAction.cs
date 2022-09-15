using Content.Server.Wires;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;

namespace Content.Server.VendingMachines;

[DataDefinition]
public sealed class VendingMachineEjectItemWireAction : BaseWireAction
{
    private VendingMachineSystem _vendingMachineSystem = default!;

    private Color _color = Color.Red;
    private string _text = "VEND";
    public override object? StatusKey { get; } = EjectWireKey.StatusKey;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;

        if (IsPowered(wire.Owner)
            && EntityManager.TryGetComponent(wire.Owner, out VendingMachineComponent? vending))
        {
            lightState = vending.CanShoot
                ? StatusLightState.BlinkingFast
                : StatusLightState.On;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }

    public override void Initialize()
    {
        base.Initialize();

        _vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent(wire.Owner, out VendingMachineComponent? vending))
        {
            _vendingMachineSystem.SetShooting(wire.Owner, true, vending);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent(wire.Owner, out VendingMachineComponent? vending))
        {
            _vendingMachineSystem.SetShooting(wire.Owner, false, vending);
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        _vendingMachineSystem.EjectRandom(wire.Owner, true);

        return true;
    }
}
