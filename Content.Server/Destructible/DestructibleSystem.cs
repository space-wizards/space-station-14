using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using DamageTrigger = Content.Shared.Destructible.Thresholds.Triggers.DamageTrigger;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public sealed class DestructibleSystem : SharedDestructibleSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        private IDependencyCollection _collection = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(Execute);
            _collection = IoCManager.Instance!;
        }

        /// <summary>
        ///     Check if any thresholds were reached. if they were, execute them.
        /// </summary>
        public void Execute(EntityUid uid, DestructibleComponent component, DamageChangedEvent args)
        {
            foreach (var threshold in component.Thresholds)
            {
                if (threshold.Reached(args.Damageable, EntityManager))
                {
                    RaiseLocalEvent(uid, new DamageThresholdReached(component, threshold), true);

                    // Convert behaviors into string for logs
                    var triggeredBehaviors = string.Join(", ", threshold.Behaviors.Select(b =>
                    {
                        if (b is DoActsBehavior doActsBehavior)
                        {
                            return $"{b.GetType().Name}:{doActsBehavior.Acts.ToString()}";
                        }
                        return b.GetType().Name;
                    }));

                    if (args.Origin != null)
                    {
                        _adminLogger.Add(LogType.Damaged, LogImpact.Medium,
                            $"{ToPrettyString(args.Origin.Value):actor} caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                    }
                    else
                    {
                        _adminLogger.Add(LogType.Damaged, LogImpact.Medium,
                            $"Unknown damage source caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                    }

                    threshold.Execute(uid, _collection, EntityManager, args.Origin);
                }

                // if destruction behavior (or some other deletion effect) occurred, don't run other triggers.
                if (EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                    return;
            }
        }

        public bool TryGetDestroyedAt(Entity<DestructibleComponent?> ent, [NotNullWhen(true)] out FixedPoint2? destroyedAt)
        {
            destroyedAt = null;
            if (!Resolve(ent, ref ent.Comp, false))
                return false;

            destroyedAt = DestroyedAt(ent, ent.Comp);
            return true;
        }

        // FFS this shouldn't be this hard. Maybe this should just be a field of the destructible component. Its not
        // like there is currently any entity that is NOT just destroyed upon reaching a total-damage value.
        /// <summary>
        ///     Figure out how much damage an entity needs to have in order to be destroyed.
        /// </summary>
        /// <remarks>
        ///     This assumes that this entity has some sort of destruction or breakage behavior triggered by a
        ///     total-damage threshold.
        /// </remarks>
        public FixedPoint2 DestroyedAt(EntityUid uid, DestructibleComponent? destructible = null)
        {
            if (!Resolve(uid, ref destructible, logMissing: false))
                return FixedPoint2.MaxValue;

            // We have nested for loops here, but the vast majority of components only have one threshold with 1-3 behaviors.
            // Really, this should probably just be a property of the damageable component.
            var damageNeeded = FixedPoint2.MaxValue;
            foreach (var threshold in destructible.Thresholds)
            {
                if (threshold.Trigger is not DamageTrigger trigger)
                    continue;

                foreach (var behavior in threshold.Behaviors)
                {
                    if (behavior is DoActsBehavior actBehavior &&
                        actBehavior.HasAct(ThresholdActs.Destruction | ThresholdActs.Breakage))
                    {
                        damageNeeded = Math.Min(damageNeeded.Float(), trigger.Damage);
                    }
                }
            }
            return damageNeeded;
        }
    }

    // Currently only used for destructible integration tests. Unless other uses are found for this, maybe this should just be removed and the tests redone.
    /// <summary>
    ///     Event raised when a <see cref="DamageThreshold"/> is reached.
    /// </summary>
    public sealed class DamageThresholdReached : EntityEventArgs
    {
        public readonly DestructibleComponent Parent;

        public readonly DamageThreshold Threshold;

        public DamageThresholdReached(DestructibleComponent parent, DamageThreshold threshold)
        {
            Parent = parent;
            Threshold = threshold;
        }
    }
}
