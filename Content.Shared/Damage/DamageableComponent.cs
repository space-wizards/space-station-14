using System;
using System.Collections.Generic;
using Content.Shared.Acts;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Radiation;
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
    ///     may also have resistances to certain damage types, defined via a <see cref="ResistanceSetPrototype"/>.
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent()]
    public class DamageableComponent : Component, IRadiationAct, IExAct
    {
        public override string Name => "Damageable";

        [DataField("damageContainer", required : true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        public readonly string DamageContainerID = default!;

        [DataField("resistanceSet", customTypeSerializer: typeof(PrototypeIdSerializer<ResistanceSetPrototype>))]
        public string? ResistanceSetID;

        /// <summary>
        ///     The main damage dictionary. All the damage information is stored in this dictionary using <see cref="DamageTypePrototype"/> IDs as keys.
        /// </summary>
        [ViewVariables] public Dictionary<string, int> DamagePerType = new();

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
            foreach (var typeID in ExplosionDamageTypeIDs)
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
        public readonly Dictionary<string, int> DamagePerType;
        public readonly string? ResistanceSetID;

        public DamageableComponentState(
            Dictionary<string, int> damagePerType,
            string? resistanceSetID) 
        {
            DamagePerType = damagePerType;
            ResistanceSetID = resistanceSetID;
        }
    }
}
