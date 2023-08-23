using Content.Shared.Bed.Sleep;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    ISawmill s = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StatusEffectsComponent, StatusEffectTimeAddedEvent>(OnDrunkUpdated);
    }
    public void OnDrunkUpdated(EntityUid uid, StatusEffectsComponent component, StatusEffectTimeAddedEvent args)
    {
        s = Logger.GetSawmill("server");
        if (!TryComp<DrunkComponent>(uid, out var drunkComp))
            return;
        if (!_statusEffectsSystem.TryGetTime(uid, DrunkKey, out var time, component))
            return;
        float timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
        drunkComp.CurrentBoozePower = timeLeft;
        s.Debug(drunkComp.CurrentBoozePower.ToString());
        if (drunkComp.CurrentBoozePower > 200f)
        {
            if (_statusEffectsSystem.HasStatusEffect(uid, StatusEffectKey))
            {
                _statusEffectsSystem.TrySetTime(uid, StatusEffectKey, TimeSpan.FromSeconds(timeLeft));
            }

            _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(3f), false);
        }
    }
}
