using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Inventory;

[Prototype("inventoryTemplate")]
public class InventoryTemplatePrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = string.Empty;

    [DataField("slots")]
    public SlotDefinition[] Slots { get; } = Array.Empty<SlotDefinition>();
}

[DataDefinition]
public class SlotDefinition
{
    [DataField("name", required: true)]
    public string Name { get; } = string.Empty;

    [DataField("displayName")]
    public string? DisplayName { get; }

    [DataField("slotTexture")]
    public SpriteSpecifier? Texture;

    //todo paul is this how you serialize flags?
    [DataField("SlotFlags")]
    public SlotFlags SlotFlags;
}
