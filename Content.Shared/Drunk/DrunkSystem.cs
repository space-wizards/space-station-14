using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;
using Content.Shared.Bed.Sleep;
using Robust.Shared.Timing;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string DrunkKey = "Drunk";

    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string SleepKey = "ForcedSleep";
    
    [Dependency] protected readonly StatusEffectsSystem StatusEffectsSystem = default!;
    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsComponent, StatusEffectEndedEvent>(OnDrunkEnded);
    }

    private void OnDrunkEnded(EntityUid uid, StatusEffectsComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == DrunkKey)
        {
            StatusEffectsSystem.TryRemoveStatusEffect(uid, SleepKey, component);
            return;
        }

        if (args.Key == SleepKey)
        {
            StatusEffectsSystem.TryRemoveStatusEffect(uid, DrunkKey, component);
            return;
        }
    }

    public void TryApplyDrunkenness(EntityUid uid, float boozePower, bool applySlur = true,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (TryComp<LightweightDrunkComponent>(uid, out var trait))
            boozePower *= trait.BoozeStrengthMultiplier;
        if (applySlur)
        {
            _slurredSystem.DoSlur(uid, TimeSpan.FromSeconds(boozePower), status);
        }

        if (!StatusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
        {
            //sometimes person metabolizes a drink more slowly than the status time is updated due to different network problems, so we add 15f to deal with it
            StatusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(15f + boozePower), true, status);
        }
        else
        {
            StatusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
        }
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        StatusEffectsSystem.TryRemoveStatusEffect(uid, DrunkKey);
    }
    public void TryRemoveDrunkenessTime(EntityUid uid, double timeRemoved)
    {
        StatusEffectsSystem.TryRemoveTime(uid, DrunkKey, TimeSpan.FromSeconds(timeRemoved));
    }
}
