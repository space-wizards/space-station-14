namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeStat : EntityEffectBase<PlantChangeStat>
{
    [DataField(required: true)]
    public string TargetValue;

    /// <summary>
    /// The plant component that contains <see cref="TargetValue"/>.
    /// </summary>
    [DataField(required: true)]
    public string TargetComponent;

    /// <summary>
    /// The minimum allowed value for the stat.
    /// </summary>
    [DataField(required: true)]
    public float MinValue;

    /// <summary>
    /// The maximum allowed value for the stat.
    /// </summary>
    [DataField(required: true)]
    public float MaxValue;

    /// <summary>
    /// Effect to apply when the stat should go up.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Up;

    /// <summary>
    /// Effect to apply when the stat should go down.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Down;
}
