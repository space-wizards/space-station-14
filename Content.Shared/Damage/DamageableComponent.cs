using System;
using System.Collections.Generic;
using Content.Shared.Acts;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Radiation;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage
{
    // TODO FRIENDS It wouldn't hurt to make friends with the damage system.

    /// <summary>
    ///     Component that allows attached entities to take damage.
    /// </summary>
    /// <remarks>
    ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
    ///     are effectively a dictionary of damage types and damage numbers, along with functions to modify them. Damage
    ///     groups are collections of damage types. This basic version never dies (thus can take an
    ///     indefinite amount of damage).
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent()]
    public class DamageableComponent : Component, IRadiationAct, ISerializationHooks, IExAct
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Damageable";

        // TODO PROTOTYPE REFERENCES
        [DataField("damageContainer", required : true)]
        public readonly string DamageContainerID = default!;

        // TODO PROTOTYPE REFERENCES
        [DataField("resistanceSet")]
        public string? ResistanceSetID;

        [ViewVariables(VVAccess.ReadWrite)]
        public ResistanceSetPrototype? ResistanceSet;

        /// <summary>
        ///     The main damage dictionary. All the damage information is stored in this dictionary using <see cref="DamageTypePrototype"/> keys.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public Dictionary<DamageTypePrototype, int> DamagePerType = new();

        /// <summary>
        ///     Damage, indexed by <see cref="DamageGroupPrototype"/> keys.
        /// </summary>
        /// <remarks>
        ///     Groups which have no members that are supported by this component will not be present in this
        ///     dictionary.
        /// </remarks>
        [ViewVariables] public Dictionary<DamageGroupPrototype, int> DamagePerGroup = new();

        /// <summary>
        ///     The sum of all damages in the DamageableComponent.
        /// </summary>
        [ViewVariables] public int TotalDamage;

        // TODO PROTOTYPE Replace these datafield variables with prototype references, once they are supported.
        // Also requires appropriate changes in OnExplosion() and RadiationAct(). Really these shouldn't be here.
        // Calculate damage in some system somewhere, pass damage onto body, then have body pass onto containers.
        [ViewVariables]
        [DataField("radiationDamageTypes")]
        public List<string> RadiationDamageTypeIDs { get; set; } = new() {"Radiation"};
        [ViewVariables]
        [DataField("explosionDamageTypes")]
        public List<string> ExplosionDamageTypeIDs { get; set; } = new() { "Piercing", "Heat" };

        // TODO RADIATION Remove this.
        void IRadiationAct.RadiationAct(float frameTime, SharedRadiationPulseComponent radiation)
        {
            var damage = Math.Max((int) (frameTime * radiation.RadsPerSecond), 1);

            // Radiation should really just be a damage group instead of a list of types.
            DamageData data = new();
            foreach (var typeID in RadiationDamageTypeIDs)
            {
                data = data + new DamageData(_prototypeManager.Index<DamageTypePrototype>(typeID), damage);
            }
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new TryChangeDamageEvent(data), false);
        }

        // TODO EXPLOSION Remove this.
        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var damage = eventArgs.Severity switch
            {
                ExplosionSeverity.Light => 20,
                ExplosionSeverity.Heavy => 60,
                ExplosionSeverity.Destruction => 250,
                _ => throw new ArgumentOutOfRangeException()
            };

            // Explosion should really just be a damage group instead of a list of types.
            DamageData data = new();
            foreach (var typeID in ExplosionDamageTypeIDs)
            {
                data = data + new DamageData(_prototypeManager.Index<DamageTypePrototype>(typeID), damage);
            }
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new TryChangeDamageEvent(data), false);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new DamageableComponentState(DamagePerType, DamagePerGroup, TotalDamage, ResistanceSet);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not DamageableComponentState state)
            {
                return;
            }

            TotalDamage = state.TotalDamage;

            // Update damage type dictionary
            foreach (var type in DamagePerType.Keys)
            {
                if (state.DamagePerTypeID.TryGetValue(type.ID, out var newValue))
                {
                    DamagePerType[type] = newValue;
                }
                else
                {
                    DamagePerType.Remove(type);
                }
            }

            // Update damage group dictionary
            foreach (var group in DamagePerGroup.Keys)
            {
                if (state.DamagePerGroupID.TryGetValue(group.ID, out var newValue))
                {
                    DamagePerGroup[group] = newValue;
                }
                else
                {
                    DamagePerGroup.Remove(group);
                }
            }

            // If the new state supports more types than before, we need to add them. There is probably a more elegant
            // way of doing this, but this scenario really shouldn't come up often, so the inefficiency shouldn't really
            // matter here.
            if ( state.DamagePerTypeID.Count > DamagePerType.Count )
            {
                foreach (var (typeID, newValue) in state.DamagePerTypeID)
                {
                    var type = _prototypeManager.Index<DamageTypePrototype>(typeID);
                    DamagePerType.TryAdd(type, newValue);
                }
            }
            if (state.DamagePerGroupID.Count > DamagePerGroup.Count)
            {
                foreach (var (groupID, newValue) in state.DamagePerGroupID)
                {
                    var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                    DamagePerGroup.TryAdd(group, newValue);
                }
            }

            // Do we need to update ResistanceSet?
            if (state.ResistanceSetID != ResistanceSet?.ID)
            {
                ResistanceSet = state.ResistanceSetID == null ? null : _prototypeManager.Index<ResistanceSetPrototype>(state.ResistanceSetID);
                ResistanceSetID = state.ResistanceSetID;
            }
        }
    }

    [Serializable, NetSerializable]
    public class DamageableComponentState : ComponentState
    {
        public readonly IReadOnlyDictionary<string, int> DamagePerTypeID;
        public readonly IReadOnlyDictionary<string, int> DamagePerGroupID;
        public readonly int TotalDamage;
        public readonly string? ResistanceSetID;

        public DamageableComponentState(
            IReadOnlyDictionary<DamageTypePrototype, int> damagePerType,
            IReadOnlyDictionary<DamageGroupPrototype, int>  damagePerGroup,
            int totalDamage,
            ResistanceSetPrototype? resistanceSet) 
        {
            // Convert prototypes to IDs for sending over the network.
            DamagePerTypeID = ConvertDictKeysToIDs(damagePerType);
            DamagePerGroupID = ConvertDictKeysToIDs(damagePerGroup);
            TotalDamage = totalDamage;

            if (resistanceSet != null)
            {
                ResistanceSetID = resistanceSet.ID;
            }
        }

        /// <summary>
        ///     Take a dictionary with <see cref="IPrototype"/> keys and return a dictionary using <see cref="IPrototype.ID"/> as keys
        ///     instead.
        /// </summary>
        /// <remarks>
        ///     Useful when sending damage type and group prototypes dictionaries over the network.
        /// </remarks>
        public static IReadOnlyDictionary<string, int>
            ConvertDictKeysToIDs<TPrototype>(IReadOnlyDictionary<TPrototype, int> prototypeDict)
            where TPrototype : IPrototype
        {
            Dictionary<string, int> idDict = new(prototypeDict.Count);
            foreach (var entry in prototypeDict)
            {
                idDict.Add(entry.Key.ID, entry.Value);
            }
            return idDict;
        }
    }
}
