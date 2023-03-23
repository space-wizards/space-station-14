using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;

namespace Content.Shared.Drunk;

public abstract class SharedDrunkSystem : EntitySystem
{
    public const string DrunkKey = "Drunk";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;

    public void TryApplyDrunkenness(EntityUid uid, float boozePower, bool applySlur = true,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (applySlur)
            _slurredSystem.DoSlur(uid, TimeSpan.FromSeconds(boozePower), status);

        if (TryComp<LightweightDrunkComponent>(uid, out var trait))
            boozePower *= trait.BoozeStrengthMultiplier;

        if (!_statusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
        }
    }
}
