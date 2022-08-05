namespace Content.Server.Mousetrap;

[RegisterComponent]
public sealed class MousetrapComponent : Component
{
    [ViewVariables]
    public bool IsActive;

    /// <summary>
    ///     Set this to change where the
    ///     inflection point in the scaling
    ///     equation will occur.
    ///     The default is 10.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("massBalance")]
    public int MassBalance = 10;
}
