using Content.Server.GameObjects.Components.Destructible.Thresholds;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible
{
    public class DestructibleThresholdReachedMessage : ComponentMessage
    {
        public DestructibleThresholdReachedMessage(DestructibleComponent parent, Threshold threshold)
        {
            Parent = parent;
            Threshold = threshold;
        }

        public DestructibleComponent Parent { get; }

        public Threshold Threshold { get; }
    }
}
