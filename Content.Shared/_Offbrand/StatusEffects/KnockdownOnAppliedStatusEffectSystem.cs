using Content.Shared.Stunnable;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class KnockdownOnAppliedStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnAppliedStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    private void OnStatusEffectApplied(Entity<KnockdownOnAppliedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _stun.TryKnockdown(args.Target, ent.Comp.Duration, true, force: true);
    }
}
