using Content.Server.Construction;
using Content.Server.Destructible.Thresholds;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Acts;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public sealed class DestructibleSystem : EntitySystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;
        public new IEntityManager EntityManager => base.EntityManager;

        [Dependency] public readonly ActSystem ActSystem = default!;
        [Dependency] public readonly AudioSystem AudioSystem = default!;
        [Dependency] public readonly ConstructionSystem ConstructionSystem = default!;
        [Dependency] public readonly ExplosionSystem ExplosionSystem = default!;
        [Dependency] public readonly StackSystem StackSystem = default!;
        [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] public readonly IComponentFactory ComponentFactory = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(Execute);
        }

        /// <summary>
        ///     Check if any thresholds were reached. if they were, execute them.
        /// </summary>
        public void Execute(EntityUid uid, DestructibleComponent component, DamageChangedEvent args)
        {
            foreach (var threshold in component.Thresholds)
            {
                if (threshold.Reached(args.Damageable, this))
                {
                    RaiseLocalEvent(uid, new DamageThresholdReached(component, threshold));

                    threshold.Execute(uid, this, EntityManager);
                }

                // if destruction behavior (or some other deletion effect) occurred, don't run other triggers.
                if (EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                    return;
            }
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
