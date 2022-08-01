using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Utility
{
    [Prototype("behaviorSet")]
    public sealed class BehaviorSetPrototype : IPrototype
    {
        /// <summary>
        ///     Name of the BehaviorSet.
        /// </summary>
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     Actions that this BehaviorSet grants to the entity.
        /// </summary>
        [DataField("actions")]
        public IReadOnlyList<string> Actions { get; private set; } = new List<string>();
    }
}
