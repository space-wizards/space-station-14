using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.PushHorn;

[RegisterComponent, NetworkedComponent]
public sealed partial class PushHornComponent : Component
{
    /// <summary>
    /// Handles how long the gravity well will remain active, null by default
    /// </summary>
    [DataField]
    public double? ToggleTime = null;
}
