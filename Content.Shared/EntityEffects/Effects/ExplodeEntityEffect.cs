using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <inheritdoc cref="EntityEffect"/>
/// <seealso cref="ExplosionEffect">
public sealed partial class ExplodeEffect : EntityEffectBase<ExplodeEffect>
{
    /// <summary>
    /// Optional override for the explosion intensity.
    /// </summary>
    [DataField]
    public float? Intensity;

    /// <summary>
    /// Optional override for the explosion radius.
    /// </summary>
    [DataField]
    public float? Radius;

    /// <summary>
    /// Delete the entity with the explosion?
    /// </summary>
    [DataField]
    public bool Delete = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-explosion", ("chance", Probability));

    public override LogImpact? Impact => LogImpact.High;
}
