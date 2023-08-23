using Content.Shared.Bed.Sleep;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
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
        if (drunkComp.CurrentBoozePower > 200f)
        {
            if (StatusEffectsSystem.HasStatusEffect(uid, StatusEffectKey))
            {
                StatusEffectsSystem.TrySetTime(uid, StatusEffectKey, TimeSpan.FromSeconds(timeLeft));
            }

            StatusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(10f), false);
        }
    }
}
