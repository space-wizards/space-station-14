using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PDA
{
    [RegisterComponent]
    public sealed class PDAComponent : Component
    {
        public const string PDAIdSlotId = "PDA-id";
        public const string PDAPenSlotId = "PDA-pen";

        [DataField("idSlot")]
        public ItemSlot IdSlot = new();

        [DataField("penSlot")]
        public ItemSlot PenSlot = new();

        // Really this should just be using ItemSlot.StartingItem. However, seeing as we have so many different starting
        // PDA's and no nice way to inherit the other fields from the ItemSlot data definition, this makes the yaml much
        // nicer to read.
        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string? OwnerName;
        [ViewVariables] public string? StationName;
    }
}
