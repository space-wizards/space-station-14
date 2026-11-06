using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Adds a destructible threshold that spills a solution and destroys the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SquashifyEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Squashify>
{
    [Dependency] private DestructibleSystem _destructible = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Squashify> args)
    {
        var threshold = new DamageThreshold
        {
            Trigger = new DamageTrigger { Damage = args.Effect.Damage },
        };

        if (args.Effect.Sound is { } sound)
            threshold.Behaviors.Add(new PlaySoundBehavior { Sound = sound });

        threshold.Behaviors.Add(new SpillBehavior { Solution = args.Effect.Solution });
        threshold.Behaviors.Add(new DoActsBehavior { Acts = ThresholdActs.Destruction });

        _destructible.AddThreshold((entity.Owner, null), threshold, null);
    }
}
