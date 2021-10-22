using System;
using System.Collections.Generic;
using Content.Shared.Acts;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Radiation;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     Component that allows entities to take damage.
    /// </summary>
    /// <remarks>
    ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
    ///     may also have resistances to certain damage types, defined via a <see cref="DamageModifierSetPrototype"/>.
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent()]
    [Friend(typeof(DamageableSystem))]
    public class DamageableComponent : Component, IRadiationAct, IExAct
    {
        public override string Name => "Damageable";

        /// <summary>
        ///     This <see cref="DamageContainerPrototype"/> specifies what damage types are supported by this component.
        ///     If null, all damage types will be supported.
        /// </summary>
        [DataField("damageContainer", customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        public string? DamageContainerID;

        /// <summary>
        ///     This <see cref="DamageModifierSetPrototype"/> will be applied to any damage that is dealt to this container,
        ///     unless the damage explicitly ignores resistances.
        /// </summary>
        /// <remarks>
        ///     Though DamageModifierSets can be deserialized directly, we only want to use the prototype version here
        ///     to reduce duplication.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damageModifierSet", customTypeSerializer: typeof(PrototypeIdSerializer<DamageModifierSetPrototype>))]
        public string? DamageModifierSetId;

        /// <summary>
        ///     All the damage information is stored in this <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     If this data-field is specified, this allows damageable components to be initialized with non-zero damage.
        /// </remarks>
        [DataField("damage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new();

        /// <summary>
        ///     Damage, indexed by <see cref="DamageGroupPrototype"/> ID keys.
        /// </summary>
        /// <remarks>
        ///     Groups which have no members that are supported by this component will not be present in this
        ///     dictionary.
        /// </remarks>
        [ViewVariables] public Dictionary<string, int> DamagePerGroup = new();

        /// <summary>
        ///     The sum of all damages in the DamageableComponent.
        /// </summary>
        [ViewVariables] public int TotalDamage;

        // Really these shouldn't be here. OnExplosion() and RadiationAct() should be handled elsewhere.
        [ViewVariables]
        [DataField("radiationDamageTypes", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageTypePrototype>))]
        public List<string> RadiationDamageTypeIDs = new() {"Radiation"};
        [ViewVariables]
        [DataField("explosionDamageTypes", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageTypePrototype>))]
        public List<string> ExplosionDamageTypeIDs = new() { "Piercing", "Heat" };

        // TODO RADIATION Remove this.
        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            var damageValue = Math.Max((int) (frameTime * radiation.RadsPerSecond), 1);

            // Radiation should really just be a damage group instead of a list of types.
            DamageSpecifier damage = new();
            foreach (var typeID in RadiationDamageTypeIDs)
            {
                damage.DamageDict.Add(typeID, damageValue);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, damage);
        }

        // TODO EXPLOSION Remove this.
        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var damageValue = eventArgs.Severity switch
            {
                ExplosionSeverity.Light => 20,
                ExplosionSeverity.Heavy => 60,
                ExplosionSeverity.Destruction => 250,
                _ => throw new ArgumentOutOfRangeException()
            };

            // Explosion should really just be a damage group instead of a list of types.
            DamageSpecifier damage = new();
            foreach (var typeID in ExplosionDamageTypeIDs)
            {
                damage.DamageDict.Add(typeID, damageValue);
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, damage);
        }
    }

    [Serializable, NetSerializable]
    public class DamageableComponentState : ComponentState
    {
        public readonly Dictionary<string, int> DamageDict;
        public readonly string? ModifierSetId;

        public DamageableComponentState(
            Dictionary<string, int> damageDict,
            string? modifierSetId)
        {
            DamageDict = damageDict;
            ModifierSetId = modifierSetId;
        }
    }
}
