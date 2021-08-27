using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Server.Containers.ItemSlots
{
    [RegisterComponent]
    public class ItemSlotsComponent : Component
    {
        public override string Name => "ItemSlots";

        [ViewVariables] [DataField("slots")] public Dictionary<string, ItemSlot> Slots = new();
    }

    [Serializable]
    [DataDefinition]
    public class ItemSlot
    {
        [ViewVariables] [DataField("whitelist")] public EntityWhitelist? Whitelist = null;
        [ViewVariables] [DataField("insertSound")] public SoundSpecifier? InsertSound;
        [ViewVariables] [DataField("ejectSound")] public SoundSpecifier? EjectSound;

        [DataField("item", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        [ViewVariables] public string? StartingItem;

        [ViewVariables] public ContainerSlot ContainerSlot = default!;
    }
}
