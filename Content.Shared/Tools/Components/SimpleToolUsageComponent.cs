using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Component responsible for simple tool interactions.
/// Using a tool with the correct quality on an entity with this component will start a doAfter and raise events.
/// </summary>
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
    /// The duration using a tool on this entity will take in seconds.
    /// </summary>
    [DataField]
    public float DoAfter = 5;

    /// <summary>
    /// What verb should display to allow you to use a tool on this entity.
    /// If null, no verb will be shown.
    /// </summary>
    [DataField]
    public LocId? UsageVerb;

    /// <summary>
    /// The message to show when the verb is disabled.
    /// </summary>
    [DataField]
    public LocId BlockedMessage = "simple-tool-usage-blocked-message";
}

[ByRefEvent]
public record struct AttemptSimpleToolUseEvent(EntityUid User, bool Cancelled = false);

[Serializable, NetSerializable]
public sealed partial class SimpleToolDoAfterEvent : SimpleDoAfterEvent;
