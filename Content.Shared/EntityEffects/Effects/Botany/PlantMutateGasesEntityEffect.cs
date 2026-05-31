namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See serverside system.
/// </summary>
public sealed partial class PlantMutateConsumeGases : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;
}

public sealed partial class PlantMutateExudeGases : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;
}
