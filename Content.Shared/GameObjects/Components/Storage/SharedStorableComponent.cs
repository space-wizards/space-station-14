#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Storage
{
    public abstract class SharedStorableComponent : Component
    {
        public override string Name => "Storable";
        public override uint? NetID => ContentNetIDs.STORABLE;

        [DataField("size")] public virtual int Size { get; set; } = 1;
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
