using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed partial class BlindnessSystem : EntitySystem
{
    public static readonly EntProtoId BlindingStatusEffect = "StatusEffectBlindness";

    [Dependency] private BlindableSystem _blindableSystem = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<BlindnessStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _blindableSystem.UpdateIsBlind(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnBlindTrySee(Entity<BlindnessStatusEffectComponent> ent, ref CanSeeAttemptEvent args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnFlashAttempt(Entity<BlindnessStatusEffectComponent> ent, ref FlashAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
