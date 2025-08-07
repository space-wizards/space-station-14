using Content.Shared.Speech.Components.AccentComponents;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public static readonly ProtoId<StatusEffectPrototype> DrunkKey = "Drunk";
    private static readonly ProtoId<StatusEffectPrototype> SlurKey = "SlurredSpeech";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public void TryApplyDrunkenness(EntityUid uid, float boozePower, bool applySlur = true,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (TryComp<LightweightDrunkComponent>(uid, out var trait))
            boozePower *= trait.BoozeStrengthMultiplier;

        if (applySlur)
        {
            ApplySlurredSpeech(uid, TimeSpan.FromSeconds(boozePower), status);
        }

        if (!_statusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
        }
    }

    public void TryRemoveDrunkenness(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, DrunkKey);
    }
    public void TryRemoveDrunkenessTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, DrunkKey, TimeSpan.FromSeconds(timeRemoved));
    }

    public void ApplySlurredSpeech(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, SlurKey, status))
            _statusEffectsSystem.TryAddStatusEffect<SlurredAccentComponent>(uid, SlurKey, time, true, status);
        else
            _statusEffectsSystem.TryAddTime(uid, SlurKey, time, status);
    }
}
