namespace Content.Server.Mousetrap;

[RegisterComponent]
public sealed partial class MousetrapComponent : Component
{
    [ViewVariables]
	[DataField("isActive")]
    public bool IsActive = false;

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
