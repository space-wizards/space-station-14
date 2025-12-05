using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

/// <summary>
/// An emoting wag for markings.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WaggingComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleWagging";

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Suffix to add to get the animated marking.
    /// </summary>
    public string Suffix = "Animated";

    /// <summary>
    /// Is the entity currently wagging.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Wagging = false;
}
