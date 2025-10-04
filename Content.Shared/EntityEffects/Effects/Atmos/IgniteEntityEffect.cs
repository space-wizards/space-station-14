using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Atmos;

// Server side system.

public sealed partial class Ignite : EntityEffectBase<Ignite>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-ignite", ("chance", Probability));

    public override bool ShouldLog => true;

    public override LogImpact LogImpact => LogImpact.Medium;
}
