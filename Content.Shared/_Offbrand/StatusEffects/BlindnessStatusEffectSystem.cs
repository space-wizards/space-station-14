using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BlindnessStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectRelayedEvent<CanSeeAttemptEvent>>(OnCanSeeAttempt);
    }

    private void OnStatusEffectApplied(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _blindable.UpdateIsBlind(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _blindable.UpdateIsBlind(args.Target);
    }

    private void OnCanSeeAttempt(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CanSeeAttemptEvent> args)
    {
        args.Args.Cancel();
    }
}
