using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Drunk;

/// <summary>
/// Handles the status effect of causing the player to walk less straight, usually combined with drunkenness/bloodloss.
/// </summary>
public sealed partial class WobblyMovementSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<WobblyMovementStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        entity.Comp.NextUpdate = _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void OnMovementWish(Entity<WobblyMovementStatusEffectComponent> entity, ref StatusEffectRelayedEvent<ModifyMovementTargetDirectionEvent> args)
    {
        if (!TryComp<StatusEffectComponent>(entity, out var statusEffect))
            return;

        if (_timing.CurTime >= entity.Comp.NextUpdate)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity), GetNetEntity(statusEffect.AppliedTo));

            entity.Comp.NextUpdate += TimeSpan.FromSeconds(rand.NextFloat(entity.Comp.UpdateIntervalIntervals.X, entity.Comp.UpdateIntervalIntervals.Y));

            // We don't scale the effect based on your movement speed because, by virtue of moving slower, the effect becomes easier to control.
            // If you move slower you end up having more time to adjust manually using movement input before drifting too far,
            // making you walk in a straighter line even without additional compensation.
            // You still get the initial deviation, but that makes for a good "stumbling" feeling that this effect is going for.

            var effectStrength = 1f;
            if (statusEffect.EndEffectTime != null)
            {
                var calcTime = Math.Min((_timing.CurTime - statusEffect.StartEffectTime - entity.Comp.DelayBufferTime).TotalSeconds,
                    (statusEffect.EndEffectTime - _timing.CurTime - entity.Comp.DelayBufferTime).Value.TotalSeconds);

                // Effect scales linearly up and down in strength to the max
                effectStrength = (float)(Math.Min(calcTime, entity.Comp.TimeUntilMax.TotalSeconds) / entity.Comp.TimeUntilMax.TotalSeconds);

                if (effectStrength < 0f)
                    effectStrength = 0f;
            }

            var newAngle = rand.NextAngle(-effectStrength * entity.Comp.MaxAngle, effectStrength * entity.Comp.MaxAngle);
            entity.Comp.CurrentAngle = newAngle;

            DirtyFields(entity.AsNullable(),
                null,
                nameof(WobblyMovementStatusEffectComponent.NextUpdate),
                nameof(WobblyMovementStatusEffectComponent.CurrentAngle));
        }

        args.Args = args.Args with
        {
            WishDir = entity.Comp.CurrentAngle.RotateVec(args.Args.WishDir),
        };
    }
}
