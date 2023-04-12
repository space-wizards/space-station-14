using Content.Shared.NukeOps;

namespace Content.Server.NukeOps.Components
{
    /// <summary>
    /// Shows information about war conditions on examine
    /// </summary>
    [RegisterComponent]
    public sealed class WarConditionOnExamineComponent : Component
    {
        /// <summary>
        /// Is item is powered? Disables examine when it doesn't powered
        /// </summary>
        public bool IsPowered;

        /// <summary>
        /// Current conditions of war
        /// </summary>
        public WarConditionStatus Status;
        
        /// <summary>
        /// Time Stamp for current status
        /// </summary>
        public TimeSpan EndTime;
        
        /// <summary>
        /// Minimal crew requirement
        /// </summary>
        public int MinCrew;
    }
}
