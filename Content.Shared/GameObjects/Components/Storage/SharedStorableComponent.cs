#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Storage
{
    public abstract class SharedStorableComponent : Component
    {
        public override string Name => "Storable";

        public override uint? NetID => ContentNetIDs.STORABLE;

        public int Size
        {
            get => _size;
            set
            {
                if (value != _size)
                {
                    _size = value;
                    Dirty();
                }
            }
        }
        private int _size;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, s => s.Size, "size", 1);
        }
    }

    [Serializable, NetSerializable]
    public class StorableComponentState : ComponentState
    {
        public int Size { get; }

        public StorableComponentState(int size) : base(ContentNetIDs.STORABLE)
        {
            Size = size;
        }
    }

    /// <summary>
    /// Enum for the storage capacity of various containers
    /// </summary>
    public enum ReferenceSizes
    {
        Wallet = 4,
        Pocket = 12,
        Box = 24,
        Belt = 30,
        Toolbox = 60,
        Backpack = 100,
        NoStoring = 9999
    }
}
