using System;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedStackComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const string SerializationCache = "stack";

        public sealed override string Name => "Stack";
        public sealed override uint? NetID => ContentNetIDs.STACK;

        [DataField("count")]
        private int _count = 30;

        [DataField("max")]
        private int _maxCount = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual int Count
        {
            get => _count;
            set
            {
                _count = value;
                if (_count <= 0)
                {
                    Owner.Delete();
                }

                Dirty();
            }
        }

        [ViewVariables]
        public int MaxCount
        {
            get => _maxCount;
            private set
            {
                _maxCount = value;
                Dirty();
            }
        }

        [ViewVariables] public int AvailableSpace => MaxCount - Count;

        [ViewVariables]
        [DataField("stackType")]
        public string StackTypeId { get; } = string.Empty;

        public StackPrototype StackType => _prototypeManager.Index<StackPrototype>(StackTypeId);

        protected override void Startup()
        {
            base.Startup();

            if (StackTypeId != string.Empty && !_prototypeManager.HasIndex<StackPrototype>(StackTypeId))
            {
                Logger.Error($"No {nameof(StackPrototype)} found with id {StackTypeId} for {Owner.Prototype?.ID ?? Owner.Name}");
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StackComponentState(Count, MaxCount);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not StackComponentState cast)
            {
                return;
            }

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
}
