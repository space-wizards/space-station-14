using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

/// <summary>
/// See serverside system
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Ignite : EntityEffectBase<Ignite>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-ignite", ("chance", Probability));

    public override LogImpact? Impact => LogImpact.Medium;
}
