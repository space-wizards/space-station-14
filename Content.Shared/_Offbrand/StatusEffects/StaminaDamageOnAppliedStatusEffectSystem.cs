using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class StaminaDamageOnAppliedStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaDamageOnAppliedStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    private void OnStatusEffectApplied(Entity<StaminaDamageOnAppliedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _stamina.TakeStaminaDamage(args.Target, ent.Comp.Damage);
    }
}
