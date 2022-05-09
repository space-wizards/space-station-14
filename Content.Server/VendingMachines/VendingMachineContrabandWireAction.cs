using Content.Server.Wires;
using Content.Shared.Wires;

namespace Content.Server.VendingMachines;

[DataDefinition]
public sealed class VendingMachineContrabandWireAction : BaseToggleWireAction
{
    private readonly Color _color = Color.Green;
    private readonly string _text = "MNGR";
    public override object? StatusKey { get; } = InternalKey.StatusKey;
    public override object? TimeoutKey { get; } = InternalKey.TimeoutKey;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner) && EntityManager.TryGetComponent(wire.Owner, out VendingMachineComponent vending))
        {
            lightState = vending.Contraband
                ? StatusLightState.BlinkingSlow
                : StatusLightState.On;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }

    public override void ToggleValue(EntityUid owner, bool setting)
    {
        if (EntityManager.TryGetComponent(owner, out VendingMachineComponent vending))
        {
            vending.Contraband = setting;
        }
    }

    public override bool GetValue(EntityUid owner)
    {
        return EntityManager.TryGetComponent(owner, out VendingMachineComponent vending) && vending.Contraband;
    }

    private enum InternalKey : byte
    {
        StatusKey,
        TimeoutKey
    }
}
