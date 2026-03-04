using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Trigger.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible;

[UsedImplicitly]
public sealed partial class DestructibleSystem : SharedDestructibleSystem
{
    [Dependency] public readonly IAdminLogManager AdminLogger = default!;
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] public readonly IRobustRandom Random = default!;
    public new IEntityManager EntityManager => base.EntityManager;

    [Dependency] public readonly AtmosphereSystem AtmosphereSystem = default!;
    [Dependency] public readonly ConstructionSystem ConstructionSystem = default!;
    [Dependency] public readonly ExplosionSystem ExplosionSystem = default!;
    [Dependency] public readonly GibbingSystem Gibbing = default!;
    [Dependency] public readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] public readonly SharedSolutionContainerSystem SolutionContainerSystem = default!;
    [Dependency] public readonly PuddleSystem PuddleSystem = default!;
    [Dependency] public readonly StackSystem StackSystem = default!;
    [Dependency] public readonly TriggerSystem TriggerSystem = default!;

    /// <summary>
    /// Minimum damage to invoke overkill behavior.
    /// </summary>
    private const int MinimumOverkill = 100;

    /// <summary>
    /// Multiplier over normal damage to invoke overkill.
    /// </summary>
    private const double OverkillMultiplier = 2.0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DestructibleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(OnDamageChanged);
    }

    /// <summary>
    /// Initialization function for <see cref="DestructibleComponent"/>, adding automatic overkill threshold.
    /// </summary>
    /// <param name="uid">The entity UID of the destructible parent.</param>
    /// <param name="component">The destructible component.</param>
    /// <param name="args">The event arguments.</param>
    private void OnMapInit(EntityUid uid, DestructibleComponent component, MapInitEvent args)
    {
        AddOverkillThreshold((uid, component));
    }

    /// <summary>
    /// Check if any thresholds were reached. if they were, execute them.
    /// </summary>
    private void OnDamageChanged(EntityUid uid, DestructibleComponent component, DamageChangedEvent args)
    {
        component.IsBroken = false;

        foreach (var threshold in component.Thresholds)
        {
            if (Triggered(threshold, (uid, args.Damageable)))
            {
                RaiseLocalEvent(uid, new DamageThresholdReached(component, threshold), true);

                var logImpact = LogImpact.Low;
                // Convert behaviors into string for logs
                var triggeredBehaviors = string.Join(", ", threshold.Behaviors.Select(behavior =>
                {
                    if (logImpact <= behavior.Impact)
                        logImpact = behavior.Impact;
                    if (behavior is DoActsBehavior doActsBehavior)
                    {
                        return $"{behavior.GetType().Name}:{doActsBehavior.Acts.ToString()}";
                    }
                    return behavior.GetType().Name;
                }));

                // If it doesn't have a humanoid component, it's probably not particularly notable?
                if (logImpact > LogImpact.Medium && !HasComp<HumanoidProfileComponent>(uid))
                    logImpact = LogImpact.Medium;

                if (args.Origin != null)
                {
                    AdminLogger.Add(LogType.Damaged,
                        logImpact,
                        $"{ToPrettyString(args.Origin.Value):actor} caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                }
                else
                {
                    AdminLogger.Add(LogType.Damaged,
                        logImpact,
                        $"Unknown damage source caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                }

                Execute(threshold, uid, args.Origin);
            }

            if (threshold.OldTriggered)
            {
                component.IsBroken |= threshold.Behaviors.Any(b => b is DoActsBehavior doActsBehavior &&
                    (doActsBehavior.HasAct(ThresholdActs.Breakage) || doActsBehavior.HasAct(ThresholdActs.Destruction)));
            }

            // if destruction behavior (or some other deletion effect) occurred, don't run other triggers.
            if (EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                return;
        }
    }

    /// <summary>
    /// Check if the given threshold should trigger.
    /// </summary>
    public bool Triggered(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner)
    {
        if (threshold.Trigger == null)
            return false;

        if (threshold.Triggered && threshold.TriggersOnce)
            return false;

        if (threshold.OldTriggered)
        {
            threshold.OldTriggered = threshold.Trigger.Reached(owner, this);
            return false;
        }

        if (!threshold.Trigger.Reached(owner, this))
            return false;

        threshold.OldTriggered = true;
        return true;
    }

    /// <summary>
    /// Check if the conditions for the given threshold are currently true.
    /// </summary>
    public bool Reached(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner)
    {
        if (threshold.Trigger == null)
            return false;

        return threshold.Trigger.Reached(owner, this);
    }

    /// <summary>
    /// Triggers this threshold.
    /// </summary>
    /// <param name="threshold">The threshold to execute.</param>
    /// <param name="owner">The entity that owns this threshold.</param>
    /// <param name="cause">The entity that caused this threshold to trigger.</param>
    public void Execute(DamageThreshold threshold, EntityUid owner, EntityUid? cause = null)
    {
        threshold.Triggered = true;

        foreach (var behavior in threshold.Behaviors)
        {
            // The owner has been deleted. We stop execution of behaviors here.
            if (!Exists(owner))
                return;

            // TODO: Replace with EntityEffects.
            behavior.Execute(owner, this, cause);
        }
    }

    /// <summary>
    /// Adds a threshold to the threshold list. If the entity does not have a destructible component, one will be added.
    /// </summary>
    /// <param name="entity">The entity to target.</param>
    /// <param name="threshold">The threshold to add.</param>
    /// <param name="index">The index at which to insert the threshold.</param>
    public void AddThreshold(Entity<DestructibleComponent?> entity, DamageThreshold threshold, int? index = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            entity.Comp = AddComp<DestructibleComponent>(entity.Owner);

        if(index is not null)
            entity.Comp.Thresholds.Insert(Math.Clamp(index.Value, 0, entity.Comp.Thresholds.Count), threshold);
        else
            entity.Comp.Thresholds.Add(threshold);
    }

    /// <summary>
    /// Adds an overkill threshold if one does not exist.
    /// </summary>
    /// <param name="entity">The entity, component tuple to target.</param>
    private void AddOverkillThreshold(Entity<DestructibleComponent> entity)
    {
        if (entity.Comp.IgnoreExcess)
            return;

        var maxTrigger = FixedPoint2.Zero;

        foreach (var threshold in entity.Comp.Thresholds)
        {
            if (threshold.Trigger is not DamageTrigger trigger)
                continue;

            foreach (var behavior in threshold.Behaviors)
            {
                // Not a destruction behavior
                if (behavior is not DoActsBehavior actBehavior || !actBehavior.HasAct(ThresholdActs.Destruction))
                    continue;

                // Already has a pure destruction behavior
                if (threshold.Behaviors.Count == 1)
                    return;

                maxTrigger = FixedPoint2.Max(maxTrigger, trigger.Damage);
            }
        }

        // No destruction behavior
        if (FixedPoint2.Zero == maxTrigger)
            return;

        var autoThreshold = new DamageThreshold
        {
            Trigger = new DamageTrigger { Damage = FixedPoint2.Max(MinimumOverkill, OverkillMultiplier * maxTrigger) },
            Behaviors = { new DoActsBehavior { Acts = ThresholdActs.Destruction } },
        };

        // Thresholds are evaluated in order, so overkill must be first to avoid triggering effects
        AddThreshold(entity.AsNullable(), autoThreshold, 0);
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
                    damageNeeded = FixedPoint2.Min(damageNeeded, trigger.Damage);
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
