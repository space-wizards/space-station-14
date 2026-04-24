using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlindnessSystem : EntitySystem
{
    public static readonly EntProtoId BlindingStatusEffect = "StatusEffectBlindness";

    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectRelayedEvent<CanSeeAttemptEvent>>(OnBlindTrySee);
        SubscribeLocalEvent<BlindnessStatusEffectComponent, StatusEffectRelayedEvent<FlashAttemptEvent>>(OnFlashAttempt);
    }

    private void OnApplied(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    private void OnRemoved(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    private void OnBlindTrySee(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CanSeeAttemptEvent> args)
    {
        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }

    private void OnFlashAttempt(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRelayedEvent<FlashAttemptEvent> args)
    {
        var ev = args.Args;
        ev.Cancelled = true;
        args.Args = ev;
    }
}
