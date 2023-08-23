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
        if (!statusEffectsSystem.TryGetTime(uid, DrunkKey, out var time, component))
            return;

        float timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
        drunkComp.CurrentBoozePower = timeLeft;

        if (drunkComp.CurrentBoozePower > 200f)
        {
            if (statusEffectsSystem.HasStatusEffect(uid, StatusEffectKey))
            {
                statusEffectsSystem.TrySetTime(uid, StatusEffectKey, TimeSpan.FromSeconds(timeLeft));
            }

            statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(3f), false);
        }
    }
}
