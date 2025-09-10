using Content.Shared.Atmos.Components;
using Content.Shared.Database;

namespace Content.Shared.EntityEffects.NewEffects.Atmos;

public abstract partial class SharedIgniteEntityEffectSystem : EntityEffectSystem<FlammableComponent, Ingite>
{
    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<Ingite> args)
    {
        // Server side effect
    }
}

public sealed class Ingite : EntityEffectBase<Ingite>
{
    public override bool ShouldLog => true;

    public override LogImpact LogImpact => LogImpact.Medium;
}
