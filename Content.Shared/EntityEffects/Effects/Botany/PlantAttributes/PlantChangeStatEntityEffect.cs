using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

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
    [DataField(required: true)]
    public CompName TargetComponent;

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

    /// <summary>
    /// For reagents using this effect. String to refer to in the guidebook for what stat is being changed
    /// </summary>
    public LocId TargetDataFieldString = "plant-attribute-unknown";

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-stat", ("stat", Loc.GetString(TargetDataFieldString)), ("up", Down), ("min", ApplyRange.Min), ("down", Up), ("max", ApplyRange.Max));
    }
}
