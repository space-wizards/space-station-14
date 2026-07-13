using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Drunk;

public abstract partial class SharedWobblyWalkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<WobblyWalkStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        Log.Info("FCCCCK");
        entity.Comp.NextUpdate = _timing.CurTime;
    }

    [SubscribeLocalEvent]
    private void OnMovementWish(Entity<WobblyWalkStatusEffectComponent> entity, ref StatusEffectRelayedEvent<MovementWishDirectionEvent> args)
    {
        Log.Info("FCCCCK1");
        if (!TryComp<StatusEffectComponent>(entity, out var statusEffect))
            return;

        Log.Info("FCCCCK2");

        if (_timing.CurTime < entity.Comp.NextUpdate)
            return;

        Log.Info("FCCCCK3");

        entity.Comp.NextUpdate += entity.Comp.UpdateInterval;

        // Effect scales linearly up and down in strength to the max
        var effectStrength = statusEffect.EndEffectTime == null ? 1f : (float)Math.Min(Math.Min((_timing.CurTime - statusEffect.StartEffectTime).TotalSeconds, (statusEffect.EndEffectTime - _timing.CurTime).Value.TotalSeconds), entity.Comp.TimeUntilMax.TotalSeconds) / entity.Comp.TimeUntilMax.TotalSeconds;

        var newAngle = _random.NextAngle(-effectStrength * entity.Comp.MaxAngle, effectStrength * entity.Comp.MaxAngle);
        entity.Comp.CurrentAngle = newAngle;

        args.Args = args.Args with
        {
            WishDir = newAngle.RotateVec(args.Args.WishDir),
        };
    }
}
