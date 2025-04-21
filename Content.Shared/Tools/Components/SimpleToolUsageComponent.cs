using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SimpleToolUsageSystem))]
public sealed partial class SimpleToolUsageComponent : Component
{
    /// <summary>
    /// Tool quality required to use a tool on this.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> Quality = "Slicing";

    /// <summary>
    /// The duration using a tool on this entity will take.
    /// </summary>
    [DataField]
    public float DoAfter = 5;

    /// <summary>
    /// What verb should display to allow you to use a tool on this entity.
    /// If null, no verb will be shown.
    /// </summary>
    [DataField]
    public LocId? UsageVerb;
}

[ByRefEvent]
public sealed partial class AttemptSimpleToolUseEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User = user;
};

[Serializable, NetSerializable]
public sealed partial class SimpleToolDoAfterEvent : SimpleDoAfterEvent;
