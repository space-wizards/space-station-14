using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

/// <summary>
/// An emoting wag for markings.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WaggingSystem)), AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class WaggingComponent : Component
{
    /// <summary>
    /// The action prototype for wagging.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionToggleWagging";

    /// <summary>
    /// The action entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Suffix to add to get the animated marking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Suffix = "Animated";

    /// <summary>
    /// Is the entity currently wagging.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Wagging = false;
}
