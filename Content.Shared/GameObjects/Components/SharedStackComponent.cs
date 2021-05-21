using System;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedStackComponent : Component, ISerializationHooks
    {
        public sealed override string Name => "Stack";
        public sealed override uint? NetID => ContentNetIDs.STACK;

        [DataField("count")]
        private int _count = 30;

        [DataField("max")]
        private int _maxCount = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string StackTypeId { get; private set; } = string.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Count
        {
            get => _count;
            set
            {
                if (_count == value)
                    return;

                var old = _count;
                _count = value;

                if (_count > MaxCount)
                    _count = MaxCount;

                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new StackCountChangedEvent(){OldCount = old, NewCount = _count});

                Dirty();
            }
        }

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

            Count = cast.Count;
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

    public class StackCountChangedEvent : EntityEventArgs
    {
        public int OldCount { get; init; }
        public int NewCount { get; init; }
    }
}
