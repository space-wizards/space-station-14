using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Fills a solution container randomly using a weighted list
    /// </summary>
    [RegisterComponent]
    public sealed class RandomFillSolutionComponent : Component
    {
        /// <summary>
        ///     Solution name which to add reagents to.
        /// </summary>
        [DataField("solution")]
        public string Solution { get; set; } = "default";

        /// <summary>
        ///     List of weights and their respective solutions.
        /// </summary>
        [DataField("randomlist")]
        public List<RandomSolutionEntry> RandomList = new();
    }

    [DataDefinition]
    public struct RandomSolutionEntry
    {
        /// <summary>
        ///     Weight for this entry.
        /// </summary>
        [DataField("weight")]
        public float Weight { get; set; }

        /// <summary>
        ///     Solution to add.
        /// </summary>
        [DataField("randreagents")]
        public Solution RandReagents { get; set; }
    }
}
