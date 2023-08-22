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
    private const string StatusEffectKey = "ForcedSleep";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;
    ISawmill s = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StatusEffectsComponent, StatusUpdatedEvent>(OnDrunkUpdated);

        s = Logger.GetSawmill("up");
    }

    private void OnDrunkUpdated(EntityUid uid, StatusEffectsComponent component, StatusUpdatedEvent args)
    {
        s.Debug("1");
        if (args.Key == DrunkKey)
        {
            if (!TryComp<DrunkComponent>(uid, out var drunkComp))
                return;
            s.Debug("2");
            s.Debug(uid.ToString());
            if (!_statusEffectsSystem.TryGetTime(uid, DrunkKey, out var time, component))
                return;
            var timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
            drunkComp.CurrentBoozePower += (timeLeft - drunkComp.CurrentBoozePower) * args.FrameTime / 16f;
            s.Debug(drunkComp.CurrentBoozePower.ToString());
            UpdateOverlay(drunkComp.CurrentBoozePower);

            if (drunkComp.CurrentBoozePower > 10f)
            {
                s.Debug("3");
                _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(3f), false);
            }
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

    public abstract void UpdateOverlay(float currentBoozePower);

}
