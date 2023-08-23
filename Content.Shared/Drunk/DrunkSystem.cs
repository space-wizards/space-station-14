using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;
using Content.Shared.Bed.Sleep;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string DrunkKey = "Drunk";

    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string StatusEffectKey = "ForcedSleep";

    [Dependency] protected readonly StatusEffectsSystem StatusEffectsSystem = default!;
    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsComponent, StatusEffectEndedEvent>(OnDrunkEnded);
    }

    private void OnDrunkEnded(EntityUid uid, StatusEffectsComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == DrunkKey)
            statusEffectsSystem.TryRemoveStatusEffect(uid, StatusEffectKey, component);
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

        if (!statusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
        {
            statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), true, status);
        }
        else
        {
            statusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
        }
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        statusEffectsSystem.TryRemoveStatusEffect(uid, DrunkKey);
    }
    public void TryRemoveDrunkenessTime(EntityUid uid, double timeRemoved)
    {
        statusEffectsSystem.TryRemoveTime(uid, DrunkKey, TimeSpan.FromSeconds(timeRemoved));
    }
}
