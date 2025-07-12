using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public static EntProtoId Drunk = "StatusEffectDrunk";
    public static EntProtoId Woozy = "StatusEffectWoozy";

    /* I have no clue why this magic number was chosen, I copied it from slur system and needed it for the overlay
    If you have a more intelligent magic number be my guest to completely explode this value.
    There were no comments as to why this value was chosen three years ago. */
    public static float MagicNumber = 1100f;

    [Dependency] protected readonly StatusEffectsSystem Status = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LightweightDrunkComponent, DrunkEvent>(OnLightweightDrinking);
    }

    public void TryApplyDrunkenness(EntityUid uid, TimeSpan boozePower)
    {
        var ev = new DrunkEvent(boozePower);
        RaiseLocalEvent(uid, ref ev);

        Status.TryAddStatusEffectDuration(uid, Drunk, ev.Duration);
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        Status.TryRemoveStatusEffect(uid, Drunk);
    }

    public void TryRemoveDrunkennessTime(EntityUid uid, TimeSpan boozePower)
    {
        Status.TryAddTime(uid, Drunk, - boozePower);
    }

    private void OnLightweightDrinking(Entity<LightweightDrunkComponent> entity, ref DrunkEvent args)
    {
        args.Duration *= entity.Comp.BoozeStrengthMultiplier;
    }

    [ByRefEvent]
    public record struct DrunkEvent(TimeSpan Duration);
}
