namespace Content.Server.Electrocution;

/// <summary>
/// Component for virtual electrocution entities (representing an in-progress shock).
/// </summary>
[RegisterComponent]
[Access(typeof(ElectrocutionSystem))]
public sealed partial class ElectrocutionComponent : Component
{
    /// <summary>
    /// The entity being electrocuted.
    /// </summary>
    [DataField("electrocuting")]
    public EntityUid Electrocuting;

/// <summary>
/// The entity causing the electrocution.
/// </summary>
    [DataField("source")]
    public EntityUid Source;

/// <summary>
/// Remaining duration of the electrocution.
/// </summary>
    [DataField("timeLeft")]
    public float TimeLeft;
}
