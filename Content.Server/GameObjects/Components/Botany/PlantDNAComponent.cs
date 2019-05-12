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
        public PlantLifecycle Lifecycle;
        [ViewVariables(VVAccess.ReadWrite)]
        public double MaxAgeInSeconds;
        [ViewVariables(VVAccess.ReadWrite)]
        public double YieldMultiplier;

        public object Clone()
        {
            return new PlantDNA
            {
                Lifecycle = (PlantLifecycle)Lifecycle.Clone(),
                MaxAgeInSeconds = MaxAgeInSeconds,
                YieldMultiplier = YieldMultiplier
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Lifecycle, "lifecycle", null);
            serializer.DataField(ref MaxAgeInSeconds, "maxAgeInSeconds", 180.0);
            serializer.DataField(ref YieldMultiplier, "yieldMultiplier", 3);
        }
    }

    /// <summary>
    /// Data relating to the plant's life & death states
    /// </summary>
    public class PlantLifecycle : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public List<PlantStage> LifecycleNodes;
        [ViewVariables(VVAccess.ReadWrite)]
        public SpriteSpecifier DeathSprite;
        [ViewVariables(VVAccess.ReadWrite)]
        public string DeathName;
        [ViewVariables(VVAccess.ReadWrite)]
        public string DeathDescription;
        [ViewVariables(VVAccess.ReadWrite)]
        public string StartNodeID;

        public object Clone()
        {
            return new PlantLifecycle
            {
                LifecycleNodes = (List<PlantStage>)LifecycleNodes.Clone(),
                DeathSprite = DeathSprite,
                DeathName = DeathName,
                DeathDescription = DeathDescription,
                StartNodeID = StartNodeID
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref LifecycleNodes, "lifecycleNodes", null);
            serializer.DataField(ref DeathSprite, "deathSprite", null);
            serializer.DataField(ref DeathName, "deathName", null);
            serializer.DataField(ref DeathDescription, "deathName", null);
            serializer.DataField(ref StartNodeID, "startNodeID", null);
        }
    }

    /// <summary>
    /// Rudimentary linked list nodes of plant stages for use in PlantLifecycle
    /// </summary>
    public class PlantStage : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string NodeID;

        [ViewVariables(VVAccess.ReadWrite)]
        public string stageName;
        [ViewVariables(VVAccess.ReadWrite)]
        public string stageDescription;

        [ViewVariables(VVAccess.ReadWrite)]
        public SpriteSpecifier Sprite;
        [ViewVariables(VVAccess.ReadWrite)]
        public HarvestDatum Harvest;

        [ViewVariables(VVAccess.ReadWrite)]
        public List<PlantStageTransition> Transitions;

        public object Clone()
        {
            return new PlantStage
            {
                NodeID = NodeID,
                stageName = stageName,
                stageDescription = stageDescription,
                Sprite = Sprite,
                Harvest = Harvest,
                Transitions = (List<PlantStageTransition>)Transitions.Clone()
            };
        }
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref NodeID, "stageID", null);

            serializer.DataField(ref stageName, "name", null);
            serializer.DataField(ref stageDescription, "description", null);

            serializer.DataField(ref Sprite, "spriteSpecifier", null);
            serializer.DataField(ref Harvest, "harvest", null);
            serializer.DataField(ref Transitions, "transitions", new List<PlantStageTransition>());
        }
    }

    public enum PlantStageTransitionCondition
    {
        StageProgress,
        TotalProgress
    }

    public class PlantStageTransition : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string targetNodeID;
        [ViewVariables(VVAccess.ReadWrite)]
        public PlantStageTransitionCondition conditionType;
        [ViewVariables(VVAccess.ReadWrite)]
        public double conditionAmount;

        public object Clone()
        {
            return new PlantStageTransition
            {
                targetNodeID = targetNodeID,
                conditionType = conditionType,
                conditionAmount = conditionAmount
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref targetNodeID, "targetNode", null);

            serializer.DataField(ref conditionType, "conditionType", PlantStageTransitionCondition.StageProgress);
            serializer.DataField(ref conditionAmount, "conditionAmount", 10.0);
        }
    }

    /// <summary>
    /// Barebones pointless class atm but we'll want to customize harvest data more later
    /// </summary>
    public class HarvestDatum : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string HarvestPrototype;
        [ViewVariables(VVAccess.ReadWrite)]
        public string HarvestTargetNode;

        public object Clone()
        {
            return new HarvestDatum
            {
                HarvestPrototype = HarvestPrototype,
                HarvestTargetNode = HarvestTargetNode
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref HarvestPrototype, "harvestPrototype", null);
            serializer.DataField(ref HarvestTargetNode, "harvestTargetNode", null);
        }
    }
}

