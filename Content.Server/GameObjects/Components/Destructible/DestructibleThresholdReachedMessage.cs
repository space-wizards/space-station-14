using Content.Server.GameObjects.Components.Destructible.Thresholds;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible
{
    public class DestructibleThresholdReachedMessage : ComponentMessage
    {
        public DestructibleThresholdReachedMessage(DestructibleComponent parent, Threshold threshold, int totalDamage, int thresholdAmount)
        {
            Parent = parent;
            Threshold = threshold;
            TotalDamage = totalDamage;
            ThresholdAmount = thresholdAmount;
        }

        public DestructibleComponent Parent { get; }

        public Threshold Threshold { get; }

        /// <summary>
        ///     The amount of total damage currently had that triggered this threshold.
        /// </summary>
        public int TotalDamage { get; }

        /// <summary>
        ///     The amount of damage at which this threshold triggers.
        /// </summary>
        public int ThresholdAmount { get; }
    }
}
