using Content.Shared.Bed.Sleep;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    public const float MaxTimeSleep = 30f;
    //sometimes person metabolizes a drink more slowly than the status time is updated due to different network problems, so MinTimeSleep should be more 10f to deal with it
    public const float MinTimeSleep = 15f;
    public const float BoozePowerForSleepLimit = 200f;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StatusEffectsComponent, StatusEffectTimeAddedEvent>(OnDrunkUpdated);
    }
    public void OnDrunkUpdated(EntityUid uid, StatusEffectsComponent component, StatusEffectTimeAddedEvent args)
    {
        if (!TryComp<DrunkComponent>(uid, out var drunkComp))
            return;
        if (!StatusEffectsSystem.TryGetTime(uid, DrunkKey, out var time, component))
            return;

        float timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
        drunkComp.CurrentBoozePower = timeLeft;
        float timeForSleep = timeLeft - BoozePowerForSleepLimit;
        if (timeForSleep > 0)
        {
            if (StatusEffectsSystem.HasStatusEffect(uid, SleepKey))
            {
                if (timeForSleep <= MaxTimeSleep)
                {
                    StatusEffectsSystem.TrySetTime(uid, SleepKey, TimeSpan.FromSeconds(timeForSleep));
                }
                return;
            }
            StatusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, SleepKey, TimeSpan.FromSeconds(MinTimeSleep), false);
        }
    }
}
