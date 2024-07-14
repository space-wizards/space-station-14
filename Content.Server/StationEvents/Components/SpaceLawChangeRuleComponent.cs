using System.Collections.Generic;
using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components
{
    [RegisterComponent, Access(typeof(SpaceLawChangeRule))]
    public sealed partial class SpaceLawChangeRuleComponent : Component
    {
        /// <summary>
        /// Localization key of a random message selected for the current event
        /// </summary>
        [DataField]
        public string? RandomMessage { get; set; }

        /// <summary>
        /// Sequence of laws to be used for the current event
        /// </summary>
        [DataField]
        public List<string> SequenceLaws { get; set; } = new();
    }
}
