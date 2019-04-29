using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
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
        public string HarvestPrototype;

        [ViewVariables(VVAccess.ReadWrite)]
        public double lifeProgressRequiredInSeconds;

        public object Clone()
        {
            return new PlantStage
            {
                NodeID = NodeID,
                Sprite = Sprite,
                HarvestPrototype = HarvestPrototype,
                lifeProgressRequiredInSeconds = lifeProgressRequiredInSeconds
            };
        }
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref NodeID, "stageID", null);

            serializer.DataField(ref Sprite, "spriteSpecifier", null);
            serializer.DataField(ref HarvestPrototype, "harvestPrototype", null);

            serializer.DataField(ref lifeProgressRequiredInSeconds, "lifeProgressRequired", 0.0);
        }
    }
}
