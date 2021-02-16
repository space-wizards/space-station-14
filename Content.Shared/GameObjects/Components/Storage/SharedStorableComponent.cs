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

        public virtual int Size { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, s => s.Size, "size", 1);
        }
    }

    [Serializable, NetSerializable]
    public class StorableComponentState : ComponentState
    {
        public readonly int Size;

        public StorableComponentState(int size) : base(ContentNetIDs.STORABLE)
        {
            Size = size;
        }
    }
}
