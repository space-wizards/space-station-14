namespace Content.Server.Genetics;

[RegisterComponent]
public sealed partial class GeneticMutationInjectorComponent : Component
{
    [DataField]
    public string MutationId = string.Empty;

    [DataField]
    public float ActiveDuration;
}
