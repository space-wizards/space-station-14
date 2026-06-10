namespace Content.Client._Offbrand.BodyVisuals;

/// <summary>
/// Allows a body's visuals to be relayed to other entities (mostly for health UIs)
/// </summary>
[RegisterComponent]
public sealed partial class BodyAppearanceRelayComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Targets = [];
}

/// <summary>
/// Raised on a body when a new relay target is added
/// </summary>
[ByRefEvent]
public readonly record struct BodyAppearanceRelayTargetAddedEvent(EntityUid Target);

/// <summary>
/// Raised on a body when a relay target is removed
/// </summary>
[ByRefEvent]
public readonly record struct BodyAppearanceRelayTargetRemovedEvent(EntityUid Target);
