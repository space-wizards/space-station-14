using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Washes the cream pie off of this entity face.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
/// TODO: This can probably be made into a generic "CleanEntityEffect" which multiple components listen to...
public sealed partial class WashCreamPieEntityEffectSystem : EntityEffectSystem<CreamPiedComponent, WashCreamPie>
{
    [Dependency] private readonly SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<WashCreamPie> args)
    {
        _creamPie.SetCreamPied(entity, entity.Comp, false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class WashCreamPie : EntityEffectBase<WashCreamPie>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-wash-cream-pie-reaction", ("chance", Probability));
}
