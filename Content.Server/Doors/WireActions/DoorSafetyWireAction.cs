using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public class DoorSafetyWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "SAFE";

    public override StatusLightData GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object Identifier { get; } = AirlockWireIdentifier.Safety;

    public override object StatusKey { get; } = AirlockWireStatus.SafetyIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {

        return true;
    }

    private void AwaitSafetyTimerFinish(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, "", false);
    }
}
