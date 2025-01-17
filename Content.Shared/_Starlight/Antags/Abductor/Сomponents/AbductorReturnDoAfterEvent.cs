using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Antags.Abductor;

[Serializable, NetSerializable]
public sealed partial class AbductorReturnDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AbductorGizmoMarkDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AbductorExtractDoAfterEvent : SimpleDoAfterEvent
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
[Serializable, NetSerializable]
public sealed partial class AbductorAttractDoAfterEvent : SimpleDoAfterEvent
{
    [DataField("coordinates", required: true)]
    public NetCoordinates TargetCoordinates;

    [DataField("victim", required: true)]
    public NetEntity Victim;
    
    [DataField("dispencer", required: true)]
    public NetCoordinates Dispencer;
    private AbductorAttractDoAfterEvent()
    {
    }

    public AbductorAttractDoAfterEvent(NetCoordinates coords, NetEntity target, NetCoordinates dispencer)
    {
        TargetCoordinates = coords;
        Victim = target;
        Dispencer = dispencer;
    }

    public override DoAfterEvent Clone() => this;
}
