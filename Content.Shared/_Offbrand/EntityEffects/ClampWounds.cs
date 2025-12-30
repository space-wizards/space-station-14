using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ClampWounds : EntityEffectBase<ClampWounds>
{
    [DataField(required: true)]
    public float Chance;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-clamp-wounds", ("probability", Probability), ("chance", Chance));
    }
}

public sealed class ClampWoundsEntityEffectSystem : EntityEffectSystem<WoundableComponent, ClampWounds>
{
    [Dependency] private readonly WoundableSystem _woundable = default!;

    protected override void Effect(Entity<WoundableComponent> ent, ref EntityEffectEvent<ClampWounds> args)
    {
        _woundable.ClampWounds(ent, args.Effect.Chance);
    }
}
