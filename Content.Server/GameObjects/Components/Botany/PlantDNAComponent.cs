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

    public class PlantLifecycle : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public List<PlantStage> LifecycleNodes;

        public object Clone()
        {
            return new PlantLifecycle
            {
                LifecycleNodes = (List<PlantStage>)LifecycleNodes.Clone()
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref LifecycleNodes, "lifecycleNodes", null);
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
        public SpriteSpecifier Sprite;
        [ViewVariables(VVAccess.ReadWrite)]
        public HarvestDatum Harvest;

        [ViewVariables(VVAccess.ReadWrite)]
        public double lifeProgressRequiredInSeconds;

        public object Clone()
        {
            return new PlantStage
            {
                NodeID = NodeID,
                Sprite = Sprite,
                Harvest = Harvest,
                lifeProgressRequiredInSeconds = lifeProgressRequiredInSeconds
            };
        }
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref NodeID, "stageID", null);

            serializer.DataField(ref Sprite, "spriteSpecifier", null);
            serializer.DataField(ref Harvest, "harvest", null);
            serializer.DataField(ref lifeProgressRequiredInSeconds, "lifeProgressRequired", 0.0);
        }
    }

    /// <summary>
    /// Barebones pointless class atm but we'll want to customize harvest data more later
    /// </summary>
    public class HarvestDatum : IExposeData, ICloneable
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string HarvestPrototype;

        public object Clone()
        {
            return new HarvestDatum
            {
                HarvestPrototype = HarvestPrototype
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref HarvestPrototype, "harvestPrototype", null);
        }
    }
}

