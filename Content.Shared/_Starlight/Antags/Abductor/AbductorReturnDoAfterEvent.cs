using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Medical.Surgery;

[Serializable, NetSerializable]
public sealed partial class AbductorReturnDoAfterEvent : SimpleDoAfterEvent
{
}
[Serializable, NetSerializable]
public sealed partial class AbductorSendYourselfDoAfterEvent : SimpleDoAfterEvent
{
    [DataField("coordinates", required: true)]
    public NetCoordinates TargetCoordinates;

    private AbductorSendYourselfDoAfterEvent()
    {
    }

    public AbductorSendYourselfDoAfterEvent(NetCoordinates coords) => TargetCoordinates = coords;
    public override DoAfterEvent Clone() => this;
}
