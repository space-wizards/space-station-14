using Content.Shared.Database;

namespace Content.Shared.EntityEffects.Effects.Atmos;

// Server side system.

public sealed partial class Ignite : EntityEffectBase<Ignite>
{
    public override bool ShouldLog => true;

    public override LogImpact LogImpact => LogImpact.Medium;
}
