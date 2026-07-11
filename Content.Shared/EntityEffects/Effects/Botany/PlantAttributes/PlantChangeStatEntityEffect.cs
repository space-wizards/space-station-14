using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeStat : EntityEffectBase<PlantChangeStat>
{
    /// <summary>
    /// The plant component that contains <see cref="TargetDataField"/>.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string TargetComponent;

    [DataField(required: true)]
    public string TargetDataField;

    /// <summary>
    /// Current values below the range apply <see cref="Up"/>, values above the range apply <see cref="Down"/>.
    /// Values inside the range are weighted toward either effect based on their position in the range.
    /// </summary>
    [DataField(required: true)]
    public MinMax ApplyRange;

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
