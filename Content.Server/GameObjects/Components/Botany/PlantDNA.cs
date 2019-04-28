using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantDNA : IExposeData, ICloneable
    {
        public PlantLifecycle Lifecycle;
        public double MaxAgeInSeconds;
        public double YieldMultiplier;

        public object Clone()
        {
            throw new NotImplementedException();
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
        public string germinationNodeId;
        public List<PlantLifecycleNode> LifecycleNodes;

        public object Clone()
        {
            return new PlantLifecycle
            {
                LifecycleNodes = (List<PlantLifecycleNode>)LifecycleNodes.Clone()
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
    public class PlantLifecycleNode : IExposeData, ICloneable
    {
        public string NodeID;
        public string NextNodeID;
        public PlantStage Stage;
        public double ProgressRequiredForNextStage;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref NodeID, "nodeID", null);
            serializer.DataField(ref NextNodeID, "nextNodeID", null);
            serializer.DataField(ref Stage, "stage", null);
            serializer.DataField(ref ProgressRequiredForNextStage, "progressRequiredForNextStage", 15.0);
        }

        public object Clone()
        {
            return new PlantLifecycleNode
            {
                NodeID = NodeID,
                NextNodeID = NextNodeID,
                Stage = (PlantStage)Stage.Clone(),
                ProgressRequiredForNextStage = ProgressRequiredForNextStage
            };
        }

    }

    /// <summary>
    /// Plant sprites and their corresponding growns are the creative bottleneck to construction of plants,
    /// so for maximum creative composeability PlantStage is the smallest unit of a plant lifecycle.
    /// TODO: Make the above sentence legible
    /// </summary>
    public class PlantStage : IExposeData, ICloneable
    {
        public SpriteSpecifier Sprite;
        public string GrownPrototype;

        public object Clone()
        {
            return new PlantStage
            {
                Sprite = Sprite,
                GrownPrototype = GrownPrototype
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Sprite, "spriteSpecifier", null);
            serializer.DataField(ref GrownPrototype, "grownPrototype", null);
        }
    }
}
