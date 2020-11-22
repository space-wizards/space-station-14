using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible
{
    public class DestructibleThresholdReachedMessage : ComponentMessage
    {
        public DestructibleThresholdReachedMessage(DestructibleComponent parent, Threshold threshold, int thresholdAmount)
        {
            Parent = parent;
            Threshold = threshold;
            ThresholdAmount = thresholdAmount;
        }

        public DestructibleComponent Parent { get; }

        public Threshold Threshold { get; }

        /// <summary>
        ///     The amount of damage that triggered this threshold.
        /// </summary>
        public int ThresholdAmount { get; }
    }
}
