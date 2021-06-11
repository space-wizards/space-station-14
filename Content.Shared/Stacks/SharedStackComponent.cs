using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stacks
{
    public abstract class SharedStackComponent : Component, ISerializationHooks
    {
        public sealed override string Name => "Stack";
        public sealed override uint? NetID => ContentNetIDs.STACK;

        [DataField("max")]
        private int _maxCount = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string StackTypeId { get; private set; } = string.Empty;

        /// <summary>
        ///     Current stack count.
        ///     Do NOT set this directly, raise the <see cref="StackChangeCountEvent"/> event instead.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("count")]
        public int Count { get; set; } = 30;

        [ViewVariables]
        public int MaxCount
        {
            get => _maxCount;
            private set
            {
                if (_maxCount == value)
                    return;

                _maxCount = value;
                Dirty();
            }
        }

        [ViewVariables]
        public int AvailableSpace => MaxCount - Count;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StackComponentState(Count, MaxCount);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not StackComponentState cast)
                return;

            // This will change the count and call events.
            EntitySystem.Get<SharedStackSystem>().SetCount(Owner.Uid, this, cast.Count);
            MaxCount = cast.MaxCount;
        }


        [Serializable, NetSerializable]
        private sealed class StackComponentState : ComponentState
        {
            public int Count { get; }
            public int MaxCount { get; }

            public StackComponentState(int count, int maxCount) : base(ContentNetIDs.STACK)
            {
                Count = count;
                MaxCount = maxCount;
            }
        }
    }
}
