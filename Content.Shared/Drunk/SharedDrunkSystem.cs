using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public static EntProtoId Drunk = "StatusEffectDrunk";

    /* I have no clue why this magic number was chosen, I copied it from slur system and needed it for the overlay
    If you have a more intelligent magic number be my guest to completely explode this value.
    There were no comments as to why this value was chosen three years ago. */
    public static float MagicNumber = 1100f;

    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    public void TryApplyDrunkenness(EntityUid uid,
        TimeSpan boozePower,
        bool applySlur = true)
    {
        // TODO: this should probably raise an event.
        if (TryComp<LightweightDrunkComponent>(uid, out var trait))
            boozePower *= trait.BoozeStrengthMultiplier;

        if (applySlur)
            _slurredSystem.DoSlur(uid, boozePower);

        _status.TryAddStatusEffect(uid, Drunk, boozePower);
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        _status.TryRemoveStatusEffect(uid, Drunk);
    }

    public void TryRemoveDrunkennessTime(EntityUid uid, TimeSpan time)
    {
        _status.TryAddTime(uid, Drunk, -time);
    }

    private void OnStatusApplied(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<DrunkComponent>(args.Target);
    }

    private void OnStatusRemoved(Entity<DrunkStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!_status.HasEffectComp<DrunkStatusEffectComponent>(args.Target))
            RemComp<DrunkComponent>(args.Target);
    }
}
