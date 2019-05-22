using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantDNAComponent : Component
    {
        public override string Name => "PlantDNA";

        // The actual DNA is contained in its own class only because my feeble mind can't come up with a way to allow for easy cloning of the component itself
        [ViewVariables(VVAccess.ReadWrite)]
        public PlantDNA DNA;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref DNA, "DNA", null);
        }
    }

    class PlantDNA : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public List<PlantDelta> deltas;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> startingDeltaIDs;

        public object Clone()
        {
            return new PlantDNA
            {
                deltas = (List<PlantDelta>)deltas.Clone(),
                startingDeltaIDs = startingDeltaIDs,
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref deltas, "deltas", new List<PlantDelta>());
            serializer.DataField(ref startingDeltaIDs, "startingDeltaIDs", null);
        }
    }

    /// <summary>
    /// A package of changes to be applied to the plant.
    /// Todo: rename most fields X -> setX?
    /// </summary>
    public class PlantDelta : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string deltaID;

        [ViewVariables(VVAccess.ReadWrite)]
        public string setName;
        [ViewVariables(VVAccess.ReadWrite)]
        public string setDescription;
        [ViewVariables(VVAccess.ReadWrite)]
        public SpriteSpecifier setSprite;

        //This kills the plant.
        [ViewVariables(VVAccess.ReadWrite)]
        public bool setDeath; // entity intact, but plant is dead
        [ViewVariables(VVAccess.ReadWrite)]
        public bool destroy; // entity deleted

        [ViewVariables(VVAccess.ReadWrite)]
        public List<HarvestDelta> harvestDeltas;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DamageDelta> damageDeltas;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<BasicTransition> basicTransitions;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref deltaID, "deltaID", null);

            serializer.DataField(ref setName, "setName", null);
            serializer.DataField(ref setDescription, "setDescription", null);
            serializer.DataField(ref setSprite, "setSprite", null);

            serializer.DataField(ref setDeath, "setDeath", false);
            serializer.DataField(ref destroy, "destroy", false);

            serializer.DataField(ref harvestDeltas, "harvestDeltas", new List<HarvestDelta>());
            serializer.DataField(ref damageDeltas, "damageDeltas", new List<DamageDelta>());

            serializer.DataField(ref basicTransitions, "basicTransitions", new List<BasicTransition>());
        }

        public PlantDelta() { }

        /// <summary>
        /// Necessary for DRY for subclasses, see https://jonsson.xyz/2016/11/24/extend-csharp-clone-method/
        /// There's no particular reason to think there'll be subclasses of this, I just wanted to try implementing it
        /// </summary>
        /// <param name="transition"></param>
        public PlantDelta(PlantDelta transition)
        {
            deltaID = transition.deltaID;
            setName = transition.setName;
            setDescription = transition.setDescription;

            if (transition.setSprite != null)
            {
                setSprite = transition.setSprite;
            }

            setDeath = transition.setDeath;
            destroy = transition.destroy;

            harvestDeltas = (List<HarvestDelta>)transition.harvestDeltas.Clone();
            damageDeltas = (List<DamageDelta>)transition.damageDeltas.Clone();

            basicTransitions = (List<BasicTransition>)transition.basicTransitions.Clone();
        }

        public object Clone()
        {
            return new PlantDelta(this);
        }
    }

    public enum HarvestTool
    {
        None,
        Sharp,
        Gloves
    }

    public class HarvestDelta : ICloneable, IExposeData, IListDelta
    {
        [ViewVariables(VVAccess.ReadWrite)]
        private string id;
        public string GetID() => id;

        [ViewVariables(VVAccess.ReadWrite)]
        private ListDeltaOperation operation;
        public ListDeltaOperation GetOperation() => operation;

        [ViewVariables(VVAccess.ReadWrite)]
        public string prototype;
        [ViewVariables(VVAccess.ReadWrite)]
        public double yield;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool sterile;
        [ViewVariables(VVAccess.ReadWrite)]
        public HarvestTool toolRequired;

        public object Clone()
        {
            return new HarvestDelta
            {
                id = id,
                operation = operation,
                prototype = prototype,
                yield = yield,
                sterile = sterile,
                toolRequired = toolRequired
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref id, "id", null);
            serializer.DataField(ref operation, "operation", ListDeltaOperation.Add);
            serializer.DataField(ref prototype, "prototype", null);
            serializer.DataField(ref yield, "yield", 1.0);
            serializer.DataField(ref sterile, "sterile", false);
            serializer.DataField(ref toolRequired, "toolRequired", HarvestTool.None);
        }
    }

    public class DamageDelta : ICloneable, IExposeData, IListDelta
    {
        [ViewVariables(VVAccess.ReadWrite)]
        private string id;
        public string GetID() => id;

        [ViewVariables(VVAccess.ReadWrite)]
        private ListDeltaOperation operation;
        public ListDeltaOperation GetOperation() => operation;


        [ViewVariables(VVAccess.ReadWrite)]
        public Shared.GameObjects.DamageType type;
        [ViewVariables(VVAccess.ReadWrite)]
        public double amountPerSecond;

        public object Clone()
        {
            return new DamageDelta
            {
                id = id,
                operation = operation,
                type = type,
                amountPerSecond = amountPerSecond
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref id, "id", null);
            serializer.DataField(ref operation, "operation", ListDeltaOperation.Add);
            serializer.DataField(ref type, "type", Shared.GameObjects.DamageType.Toxic);
            serializer.DataField(ref amountPerSecond, "amount", 5.0);
        }
    }

    public enum ListDeltaOperation
    {
        Add,
        Replace,
        Remove,
        RemoveAllOfType,
        RemoveAll
    }

    public interface IListDelta
    {
        ListDeltaOperation GetOperation();
        string GetID();
    }

    public enum BasicTransitionCondition
    {
        Progress,
        CellularAge,
        DeathThreshold, // Change this to a damage quantity based transition when Damageablecomponent allows for adding thresholds after init
        // or do it the hacky, less responsive way and just manually check the amount of damage every time
        Harvested
    }

    public class BasicTransition : ICloneable, IExposeData, IListDelta
    {
        [ViewVariables(VVAccess.ReadWrite)]
        private ListDeltaOperation operation;
        public ListDeltaOperation GetOperation() => operation;

        [ViewVariables(VVAccess.ReadWrite)]
        private string transitionID;
        public string GetID() => transitionID;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> DeltaIDs;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<PlantDelta> Deltas;

        [ViewVariables(VVAccess.ReadWrite)]
        public BasicTransitionCondition conditionType;
        [ViewVariables(VVAccess.ReadWrite)]
        public double conditionAmount;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool conditionIsAbsolute;

        public object Clone()
        {
            return new BasicTransition
            {
                operation = operation,
                transitionID = transitionID,

                DeltaIDs = (List<string>)DeltaIDs.Clone(),
                Deltas = (List<PlantDelta>)Deltas.Clone(),

                conditionType = conditionType,
                conditionAmount = conditionAmount,
                conditionIsAbsolute = conditionIsAbsolute
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref transitionID, "transitionID", null);
            serializer.DataField(ref operation, "operation", ListDeltaOperation.Add);

            serializer.DataField(ref DeltaIDs, "deltaIDs", new List<string>());
            serializer.DataField(ref Deltas, "deltas", new List<PlantDelta>());

            serializer.DataField(ref conditionType, "conditionType", BasicTransitionCondition.Progress);
            serializer.DataField(ref conditionAmount, "conditionAmount", 0.0);
            serializer.DataField(ref conditionIsAbsolute, "conditionIsAbsolute", false);
        }
    }
}

