using Robust.Shared.GameStates;

namespace Content.Shared.Mousetrap;

[RegisterComponent, NetworkedComponent]
public sealed partial class MousetrapComponent : Component
{
    [ViewVariables]
    [DataField]
    public bool IsActive = false;

    /// <summary>
    ///     Set this to change where the
    ///     inflection point in the scaling
    ///     equation will occur.
    ///     The default is 10.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MassBalance = 10;
}
