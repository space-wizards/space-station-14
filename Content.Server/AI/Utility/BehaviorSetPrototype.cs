using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.AI.Utility
{
    [Prototype("behaviorSet")]
    public class BehaviorSetPrototype : IPrototype
    {
        /// <summary>
        ///     Name of the BehaviorSet.
        /// </summary>
        [DataField("id", required: true)]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Actions that this BehaviorSet grants to the entity.
        /// </summary>
        [DataField("actions")]
        public IReadOnlyList<string> Actions { get; private set; }
    }
}
