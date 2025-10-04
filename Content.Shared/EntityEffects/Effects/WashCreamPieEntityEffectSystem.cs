using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class WashCreamPieEntityEffectSystem : EntityEffectSystem<CreamPiedComponent, WashCreamPie>
{
    [Dependency] private readonly SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<WashCreamPie> args)
    {
        _creamPie.SetCreamPied(entity, entity.Comp, false);
    }
}

public sealed partial class WashCreamPie : EntityEffectBase<WashCreamPie>
{
    ///<inhereitdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-wash-cream-pie-reaction", ("chance", Probability));
}
