using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Wagging;

/// <summary>
/// An emoting wag for markings.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WaggingComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleWagging";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public ProtoId<EmotePrototype> EmoteId = "WagTail";

    /// <summary>
    /// Suffix to add to get the animated marking.
    /// </summary>
    public string Suffix = "Animated";

    /// <summary>
    /// Is the entity currently wagging.
    /// </summary>
    [DataField]
    public bool Wagging = false;
}
