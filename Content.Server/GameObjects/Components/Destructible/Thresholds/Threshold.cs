#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds
{
    public class Threshold : IExposeData
    {
        [ViewVariables] private List<IThresholdBehavior> _behaviors = new();

        [ViewVariables] public int? Damage;

        [ViewVariables] public Dictionary<DamageClass, int>? DamageClasses;

        [ViewVariables] public Dictionary<DamageType, int>? DamageTypes;

        [ViewVariables] public int DamageTotal =>
            Damage +
            DamageClasses?.Values.Sum() ?? 0 +
            DamageTypes?.Values.Sum() ?? 0;

        /// <summary>
        ///     Whether or not <see cref="Damage"/>, <see cref="DamageClasses"/> and
        ///     <see cref="DamageTypes"/> all have to be met in order to reach this state,
        ///     or just one of them.
        /// </summary>
        [ViewVariables] public bool Inclusive = true;

        /// <summary>
        ///     Whether or not this threshold has already been triggered.
        /// </summary>
        [ViewVariables] public bool Triggered { get; private set; }

        /// <summary>
        ///     Whether or not this threshold only triggers once.
        ///     If false, it will trigger again once the entity is healed
        ///     and then damaged to reach this threshold once again.
        ///     It will not repeatedly trigger as damage rises beyond that.
        /// </summary>
        [ViewVariables] public bool TriggersOnce { get; set; }

        /// <summary>
        ///     Behaviors to activate once this threshold is triggered.
        /// </summary>
        [ViewVariables] public IReadOnlyList<IThresholdBehavior> Behaviors => _behaviors;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Damage, "damage", null);
            serializer.DataField(ref DamageClasses, "damageClasses", null);
            serializer.DataField(ref DamageTypes, "damageTypes", null);
            serializer.DataField(ref Inclusive, "inclusive", true);
            serializer.DataField(this, x => x.Triggered, "triggered", false);
            serializer.DataField(this, x => x.TriggersOnce, "triggersOnce", false);
            serializer.DataField(ref _behaviors, "behaviors", new List<IThresholdBehavior>());
        }

        private bool DamageClassesReached(IReadOnlyDictionary<DamageClass, int>? classesReached)
        {
            if (DamageClasses == null)
            {
                return true;
            }

            if (classesReached == null)
            {
                return false;
            }

            foreach (var (@class, damageRequired) in DamageClasses)
            {
                if (!classesReached.TryGetValue(@class, out var damageReached) ||
                    damageReached < damageRequired)
                {
                    return false;
                }
            }

            return true;
        }

        private bool DamageTypesReached(IReadOnlyDictionary<DamageType, int>? typesReached)
        {
            if (DamageTypes == null)
            {
                return true;
            }

            if (typesReached == null)
            {
                return false;
            }

            foreach (var (type, damageRequired) in DamageTypes)
            {
                if (!typesReached.TryGetValue(type, out var damageReached) ||
                    damageReached < damageRequired)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Reached(
            int? damage = null,
            IReadOnlyDictionary<DamageClass, int>? damageClasses = null,
            IReadOnlyDictionary<DamageType, int>? damageTypes = null)
        {
            if (Inclusive)
            {
                return damage >= Damage &&
                       DamageClassesReached(damageClasses) &&
                       DamageTypesReached(damageTypes);
            }
            else
            {
                return damage >= Damage ||
                       DamageClassesReached(damageClasses) ||
                       DamageTypesReached(damageTypes);
            }
        }

        /// <summary>
        ///     Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to get dependency and
        ///     system references from, if relevant.
        /// </param>
        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            Triggered = true;

            foreach (var behavior in Behaviors)
            {
                // The owner has been deleted. We stop execution of behaviors here.
                if (owner.Deleted)
                    return;

                behavior.Trigger(owner, system);
            }
        }
    }
}
