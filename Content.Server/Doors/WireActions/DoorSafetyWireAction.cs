using Content.Server.Wires;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public class DoorBoltWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor;

    [DataField("name")]
    private string _text = "BLT";

    public override StatusLightData GetStatusLightData(Wire wire)
    {

    }

    public override object Identifier { get; }

    public override object StatusKey { get; }

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
}
