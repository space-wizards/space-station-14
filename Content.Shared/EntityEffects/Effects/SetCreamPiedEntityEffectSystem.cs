using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class SetCreamPiedEntityEffectSystem : EntityEffectSystem<CreamPiedComponent, SetCreamPied>
{
    [Dependency] private SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<SetCreamPied> args)
    {
        _creamPie.SetCreamPied(entity.AsNullable(), args.Effect.Enabled);
    }
}

public sealed partial class SetCreamPied : EntityEffectBase<SetCreamPied>
{
    [DataField(required: true)]
    public bool Enabled;
}
