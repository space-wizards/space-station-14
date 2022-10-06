using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Stacks
{
    [NetworkedComponent, Access(typeof(SharedStackSystem))]
    public abstract class SharedStackComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string? StackTypeId { get; private set; }

        /// <summary>
        ///     Current stack count.
        ///     Do NOT set this directly, use the <see cref="SharedStackSystem.SetCount"/> method instead.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("count")]
        public ulong Count { get; set; } = 30;

        /// <summary>
        ///     Max amount of things that can be in the stack.
        ///     Overrides the max defined on the stack prototype.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("maxCountOverride")]
        public ulong? MaxCountOverride  { get; set; }

        /// <summary>
        ///     Set to true to not reduce the count when used.
        /// </summary>
        [DataField("unlimited")]
        [ViewVariables(VVAccess.ReadOnly)]
        public bool Unlimited { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class StackComponentState : ComponentState
    {
        public ulong Count { get; }
        public ulong MaxCount { get; }

        public StackComponentState(ulong count, ulong maxCount)
        {
            Count = count;
            MaxCount = maxCount;
        }
    }
}
