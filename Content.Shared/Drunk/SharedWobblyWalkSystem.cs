using Content.Shared.Movement.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Drunk;

public sealed partial class SharedWobblyWalkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<WobblyWalkStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        entity.Comp.NextUpdate = _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void OnMovementWish(Entity<WobblyWalkStatusEffectComponent> entity, ref StatusEffectRelayedEvent<MovementWishDirectionEvent> args)
    {
        if (!TryComp<StatusEffectComponent>(entity, out var statusEffect))
            return;

        if (_timing.CurTime >= entity.Comp.NextUpdate)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity), GetNetEntity(statusEffect.AppliedTo));

            entity.Comp.NextUpdate += TimeSpan.FromSeconds(rand.NextFloat(entity.Comp.UpdateIntervalIntervals.X, entity.Comp.UpdateIntervalIntervals.Y));

            // Effect scales linearly up and down in strength to the max
            var effectStrength = statusEffect.EndEffectTime == null
                ? 1f
                : (float)Math.Min(Math.Min((_timing.CurTime - statusEffect.StartEffectTime).TotalSeconds,
                        (statusEffect.EndEffectTime - _timing.CurTime).Value.TotalSeconds),
                    entity.Comp.TimeUntilMax.TotalSeconds) / entity.Comp.TimeUntilMax.TotalSeconds;

            var newAngle = rand.NextAngle(-effectStrength * entity.Comp.MaxAngle, effectStrength * entity.Comp.MaxAngle);
            entity.Comp.CurrentAngle = newAngle;

            Dirty(entity);
        }

        args.Args = args.Args with
        {
            WishDir = entity.Comp.CurrentAngle.RotateVec(args.Args.WishDir),
        };
    }
}
