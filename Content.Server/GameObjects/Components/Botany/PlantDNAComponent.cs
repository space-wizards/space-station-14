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

        [ViewVariables(VVAccess.ReadWrite)]
        public bool clearHarvests;
        [ViewVariables(VVAccess.ReadWrite)]
        public string setHarvestPrototype;

        //This kills the plant.
        [ViewVariables(VVAccess.ReadWrite)]
        public bool setDeath; // entity intact, but plant is dead
        [ViewVariables(VVAccess.ReadWrite)]
        public bool destroy; // entity deleted

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

            serializer.DataField(ref clearHarvests, "clearHarvests", false);
            serializer.DataField(ref setHarvestPrototype, "setHarvestPrototype", null);

            serializer.DataField(ref setDeath, "setDeath", false);
            serializer.DataField(ref destroy, "destroy", false);

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

            clearHarvests = transition.clearHarvests;
            setHarvestPrototype = transition.setHarvestPrototype;

            setDeath = transition.setDeath;
            destroy = transition.destroy;

            damageDeltas = (List<DamageDelta>)transition.damageDeltas.Clone();

            basicTransitions = (List<BasicTransition>)transition.basicTransitions.Clone();
        }

        public object Clone()
        {
            return new PlantDelta(this);
        }
    }

    public class DamageDelta : ICloneable, IExposeData
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public Shared.GameObjects.DamageType type;
        [ViewVariables(VVAccess.ReadWrite)]
        public double amountPerSecond;

        public object Clone()
        {
            return new DamageDelta
            {
                type = type,
                amountPerSecond = amountPerSecond
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref type, "type", Shared.GameObjects.DamageType.Toxic);
            serializer.DataField(ref amountPerSecond, "amount", 5.0);
        }
    }

    public enum BasicTransitionOperation
    {
        Add,
        Replace,
        Remove,
        RemoveAllOfType,
        RemoveAll
    }

    public enum BasicTransitionCondition
    {
        Progress,
        CellularAge,
        DeathThreshold, // Change this to a damage quantity based transition when Damageablecomponent allows for adding thresholds after init
        // or do it the hacky, less responsive way and just manually check the amount of damage every time
        Harvested
    }

    public class BasicTransition : ICloneable, IExposeData
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public BasicTransitionOperation operation;

        [ViewVariables(VVAccess.ReadWrite)]
        public string transitionID;

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
            serializer.DataField(ref operation, "operation", BasicTransitionOperation.Add);

            serializer.DataField(ref DeltaIDs, "deltaIDs", new List<string>());
            serializer.DataField(ref Deltas, "deltas", new List<PlantDelta>());

            serializer.DataField(ref conditionType, "conditionType", BasicTransitionCondition.Progress);
            serializer.DataField(ref conditionAmount, "conditionAmount", 0.0);
            serializer.DataField(ref conditionIsAbsolute, "conditionIsAbsolute", false);
        }
    }
}

