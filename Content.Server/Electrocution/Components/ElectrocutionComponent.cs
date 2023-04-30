namespace Content.Server.Electrocution;

/// <summary>
/// Component for virtual electrocution entities (representing an in-progress shock).
/// </summary>
[RegisterComponent]
[Access(typeof(ElectrocutionSystem))]
public sealed class ElectrocutionComponent : Component
{
    [DataField("timeLeft")]
    public float TimeLeft;

    [DataField("electrocuting")]
    public EntityUid Electrocuting;

    [DataField("accumDamage")]
    public float AccumulatedDamage;

    [DataField("source")]
    public EntityUid Source;
}
