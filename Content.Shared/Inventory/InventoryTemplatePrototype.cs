using System;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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
    [DataField("name", required: true)] public string Name { get; } = string.Empty;

    [DataField("slotTexture")] public string TextureName { get; } = "pocket";

    [DataField("slotFlags")] public SlotFlags SlotFlags { get; } = SlotFlags.PREVENTEQUIP;

    [DataField("uiContainer")] public SlotUIContainer UIContainer { get; } = SlotUIContainer.None;

    [DataField("uiWindowPos", required: true)] public Vector2i UIWindowPosition { get; }

    //todo this is supreme shit and ideally slots should be stored in a given equipmentslotscomponent on each equipment
    [DataField("dependsOn")] public string? DependsOn { get; }

    [DataField("displayName", required: true)] public string DisplayName { get; } = string.Empty;
}

public enum SlotUIContainer
{
    None,
    BottomLeft,
    BottomRight,
    Top
}
