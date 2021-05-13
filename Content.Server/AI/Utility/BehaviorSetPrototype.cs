#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.AI.Utility
{
    [Prototype("behaviorSet")]
    public class BehaviorSetPrototype : IPrototype
    {
        /// <summary>
        ///     Name of the BehaviorSet.
        /// </summary>
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     Actions that this BehaviorSet grants to the entity.
        /// </summary>
        [DataField("actions")]
        public IReadOnlyList<string> Actions { get; private set; } = new List<string>();
    }
}
