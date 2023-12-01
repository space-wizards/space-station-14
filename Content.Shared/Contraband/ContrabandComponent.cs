using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Contraband;

/// <summary>
/// Identifies items classed as contrband.
/// </summary>

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerComponent : Component
{
    /// <summary>
    ///     Whether or not the item is stealthy.
    /// </summary>
    [DataField("stealth"), ViewVariables(VVAccess.ReadWrite)]
    public bool stealth;
}
