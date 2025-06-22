namespace Content.Shared.AirTank;

/// <summary>
///     Specifies what kinds of air tanks someone should be breathing from.
/// </summary>
[RegisterComponent]
public sealed partial class AirTankShouldBreatheComponent : Component
{
    /// <summary>
    ///     What this species should be able to breathe.
    /// </summary>
    [DataField]
    public List<AirTankLooksLike> TankTypes = new();

    /// <summary>
    ///     The last tank we toggled.
    /// </summary>
    [DataField]
    public EntityUid? LastAttempted;
}
