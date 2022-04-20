using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public class DoorTimingWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor;

    [DataField("name")]
    private string _text = "TIMR";

    public override StatusLightData GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object Identifier { get; } = AirlockWireIdentifier.Timing;

    public override object StatusKey { get; } = AirlockWireStatus.TimingIndicator;

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

    // timing timer??? ???
    private void AwaitTimingTimerFinish(Wire wire)
    {
        WiresSystem.SetData(wire.Owner, "", false);
    }

}
