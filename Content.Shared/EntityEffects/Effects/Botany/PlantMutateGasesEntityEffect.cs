namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See serverside system.
/// </summary>
public sealed partial class PlantMutateConsumeGases : EntityEffect
{
    /// <summary>
    /// When actualizing this mutation, this value is the minimum volume of gas, the plant could consume.
    /// </summary>
    [DataField]
    public float MinValue = 0.01f;

    /// <summary>
    /// When actualizing this mutation, this value is the maximum volume of gas the plant could consume.
    /// </summary>
    [DataField]
    public float MaxValue = 0.5f;
}

public sealed partial class PlantMutateExudeGases : EntityEffect
{
    /// <summary>
    /// When actualizing this mutation, this value is the minimum volume of gas, the plant could produce.
    /// </summary>
    [DataField]
    public float MinValue = 0.01f;

    /// <summary>
    /// When actualizing this mutation, this value is the minimum volume of gas the plant could produce.
    /// </summary>
    [DataField]
    public float MaxValue = 0.5f;
}
