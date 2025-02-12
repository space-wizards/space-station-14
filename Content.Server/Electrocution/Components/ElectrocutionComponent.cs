namespace Content.Server.Electrocution;

/// <summary>
/// Component for virtual electrocution entities (representing an in-progress shock).
/// </summary>
[RegisterComponent]
[Access(typeof(ElectrocutionSystem))]
public sealed partial class ElectrocutionComponent : Component
{
    [DataField]
    public EntityUid Electrocuting;

    [DataField]
    public EntityUid Source;

    [DataField]
    public float TimeLeft;
}
