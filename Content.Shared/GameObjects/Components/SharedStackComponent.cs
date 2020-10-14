using System;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedStackComponent : Component
    {
        private const string SerializationCache = "stack";

        public sealed override string Name => "Stack";
        public sealed override uint? NetID => ContentNetIDs.STACK;

        private int _count;
        private int _maxCount;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual int Count
        {
            get => _count;
            set
            {
                _count = value;
                if (_count <= 0)
                {
                    if (ContainerHelpers.TryGetContainerMan(Owner, out var containerManager))
                    {
                        containerManager.Remove(Owner);
                    }
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

        [ViewVariables] public object StackType { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _maxCount, "max", 50);
            serializer.DataFieldCached(ref _count, "count", MaxCount);

            if (serializer.Writing)
            {
                return;
            }

            if (serializer.TryGetCacheData(SerializationCache, out object stackType))
            {
                StackType = stackType;
                return;
            }

            if (serializer.TryReadDataFieldCached("stacktype", out string raw))
            {
                var refl = IoCManager.Resolve<IReflectionManager>();
                if (refl.TryParseEnumReference(raw, out var @enum))
                {
                    stackType = @enum;
                }
                else
                {
                    stackType = raw;
                }
            }
            else
            {
                stackType = Owner.Prototype.ID;
            }

            serializer.SetCacheData(SerializationCache, stackType);
            StackType = stackType;
        }

        public override ComponentState GetComponentState()
        {
            return new StackComponentState(Count, MaxCount);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is StackComponentState cast))
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

    public enum StackType
    {
        Metal,
        Glass,
        Plasteel,
        Cable,
        MVCable,
        HVCable,
        Gold,
        Phoron,
        Ointment,
        Gauze,
        Brutepack,
        FloorTileSteel,
        FloorTileCarpet,
        FloorTileWhite,
        FloorTileDark,
        FloorTileWood
    }
}
