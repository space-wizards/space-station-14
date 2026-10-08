using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Makes a item activate a cursor offset if its help in hands
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CursorOffsetInHandComponent : Component
{
    /// <summary>
    /// If it should only activate the offset if held in the active hand
    /// </summary>
    [DataField]
    public bool UseActiveHand = true;
}
