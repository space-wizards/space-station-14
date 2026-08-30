using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Flammable : EntityEffectBase<Flammable>
{
    /// <summary>
    /// Fire stack multiplier applied on an entity,
    /// unless that entity is already on fire and <see cref="MultiplierOnExisting"/> is not null.
    /// </summary>
    [DataField]
    public float Multiplier = 0.05f;

    /// <summary>
    /// Fire stack multiplier applied if the entity is already on fire. Defaults to <see cref="Multiplier"/> if null.
    /// </summary>
    [DataField]
    public float? MultiplierOnExisting;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-flammable-reaction", ("chance", Probability));

    public override LogImpact? Impact => LogImpact.Low;
}
