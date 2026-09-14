using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Starts the entity's trigger timer, if it has one.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TimerStartEntityEffectSystem : EntityEffectSystem<TimerTriggerComponent, TimerStart>
{
    [Dependency] private TriggerSystem _trigger = default!;

    protected override void Effect(Entity<TimerTriggerComponent> entity, ref EntityEffectEvent<TimerStart> args)
    {
        _trigger.ActivateTimerTrigger(entity.AsNullable(), args.User);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TimerStart : EntityEffectBase<TimerStart>;
