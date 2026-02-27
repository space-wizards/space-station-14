using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public static EntProtoId Drunk = "StatusEffectDrunk";

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
