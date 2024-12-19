using Content.Shared.Damage;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Destructible.Thresholds.Triggers;

namespace Content.Shared.Destructible.Thresholds
{
    [DataDefinition]
    public sealed partial class DamageThreshold
    {
        [DataField("behaviors", serverOnly: true)]
        private List<IThresholdBehavior> _behaviors = new();

        /// <summary>
        ///     Whether or not this threshold was triggered in the previous call to
        ///     <see cref="Reached"/>.
        /// </summary>
        [ViewVariables] public bool OldTriggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [DataField]
        public bool Triggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [DataField]
        public bool TriggersOnce;

        /// <summary>
        ///     The trigger that decides if this threshold has been reached.
        /// </summary>
        [DataField]
        public IThresholdTrigger? Trigger;

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public IReadOnlyList<IThresholdBehavior> Behaviors => _behaviors;

        public bool Reached(DamageableComponent damageable, EntityManager entManager)
        {
            if (Trigger == null)
            {
                return false;
            }

            if (Triggered && TriggersOnce)
            {
                return false;
            }

            if (OldTriggered)
            {
                OldTriggered = Trigger.Reached(damageable, entManager);
                return false;
            }

            if (!Trigger.Reached(damageable, entManager))
            {
                return false;
            }

            OldTriggered = true;
            return true;
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        public void Execute(EntityUid owner, IDependencyCollection collection, EntityManager entityManager, EntityUid? cause)
        {
            Triggered = true;

            foreach (var behavior in Behaviors)
            {
                // The owner has been deleted. We stop execution of behaviors here.
                if (!entityManager.EntityExists(owner))
                    return;

                behavior.Execute(owner, collection, entityManager, cause);
            }
        }
    }
}
