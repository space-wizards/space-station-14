using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class WashCreamPieEntityEffectSystem : EntityEffectSystem<CreamPiedComponent, WashCreamPie>
{
    [Dependency] private readonly SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<WashCreamPie> args)
    {
        _creamPie.SetCreamPied(entity, entity.Comp, false);
    }
}

[DataDefinition]
public sealed partial class WashCreamPie : EntityEffectBase<WashCreamPie>;
