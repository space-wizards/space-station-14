using Robust.Shared.GameStates;

namespace Content.Shared.Burial.Components;

/// <summary>
/// A component for graves in the process of being dug/filled
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveGraveComponent : Component
{
    /// <summary>
    /// Is this grave being dug by someone inside?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool DiggingSelfOut = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Stream;
}
