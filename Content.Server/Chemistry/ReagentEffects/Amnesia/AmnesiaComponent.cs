namespace Content.Server.Chemistry.ReagentEffects.Amnesia;

[RegisterComponent]
public sealed partial class AmnesiaComponent : Component
{
    /// <summary>
    /// The time in seconds left until the entity gets force ghosted.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilForget = TimeSpan.FromSeconds(180);

    [ViewVariables(VVAccess.ReadWrite)]
    public int Stage = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastTime;
}
