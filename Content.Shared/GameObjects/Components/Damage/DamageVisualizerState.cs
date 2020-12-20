#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Damage
{
    public class DamageVisualizerState : IExposeData
    {
        [ViewVariables] public int? Damage { get; private set; }

        [ViewVariables] public Dictionary<DamageClass, int>? DamageClasses { get; private set; }

        [ViewVariables] public Dictionary<DamageType, int>? DamageTypes { get; private set; }

        [ViewVariables] public int DamageTotal =>
            Damage +
            DamageClasses?.Values.Sum() ?? 0 +
            DamageTypes?.Values.Sum() ?? 0;

        [ViewVariables] public string? Sprite { get; private set; }

        [ViewVariables] public string? State { get; private set; }

        [ViewVariables] public int? Layer { get; private set; }

        /// <summary>
        ///     Whether or not <see cref="Damage"/>, <see cref="DamageClasses"/> and
        ///     <see cref="DamageTypes"/> all have to be met in order to reach this state,
        ///     or just one of them.
        /// </summary>
        [ViewVariables] public bool Inclusive = true;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Damage, "damage", null);
            serializer.DataField(this, x => x.DamageClasses, "damageClasses", null);
            serializer.DataField(this, x => x.DamageTypes, "damageTypes", null);
            serializer.DataField(this, x => x.Sprite, "sprite", null);
            serializer.DataField(this, x => x.State, "state", null);
            serializer.DataField(this, x => x.Layer, "layer", null);
            serializer.DataField(this, x => x.Inclusive, "inclusive", true);
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
    }
}
