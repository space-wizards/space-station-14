namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See serverside system.
/// </summary>
public sealed partial class PlantMutateConsumeGases : EntityEffectBase<PlantMutateConsumeGases>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;
}

public sealed partial class PlantMutateExudeGases : EntityEffectBase<PlantMutateExudeGases>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;
}
