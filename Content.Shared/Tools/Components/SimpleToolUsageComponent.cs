using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Component responsible for simple tool interactions.
/// Using a tool with the correct quality on an entity with this component will start a DoAfter and raise the <see cref="SimpleToolDoAfterEvent"/> other systems can subscribe to.
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

/// <summary>
/// Cancelable event that can be used to prevent tool interaction.
/// </summary>
[ByRefEvent]
public record struct AttemptSimpleToolUseEvent(EntityUid User, bool Cancelled = false);

/// <summary>
/// Raised after the right tool is used on an entity with <see cref="SimpleToolUsageComponent"/>
/// and the DoAfter has finished.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SimpleToolDoAfterEvent : SimpleDoAfterEvent;
