using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public sealed class DoorBoltWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "BOLT";
    protected override string Text
    {
        get => _text;
        set => _text = value;
    }

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner)
            && EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            if (door.BoltsDown)
            {
                lightState = StatusLightState.On;
            }
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object StatusKey { get; } = AirlockWireStatus.BoltIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var airlock))
        {
            EntityManager.System<SharedAirlockSystem>().SetBoltWireCut(airlock, true);
            if (!airlock.BoltsDown && IsPowered(wire.Owner))
                EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, airlock, true);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
            EntityManager.System<SharedAirlockSystem>().SetBoltWireCut(door, true);

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            if (IsPowered(wire.Owner))
            {
                EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, door, !door.BoltsDown);
            }
            else if (!door.BoltsDown)
            {
                EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, door, true);
            }

        }

        return true;
    }
}
