using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

[Prototype("inventoryTemplate")]
public sealed class InventoryTemplatePrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = string.Empty;

    [DataField("slots")]
    public SlotDefinition[] Slots { get; } = Array.Empty<SlotDefinition>();
}

[DataDefinition]
public sealed class SlotDefinition
{
    [DataField("name", required: true)] public string Name { get; } = string.Empty;

    [DataField("slotTexture")] public string TextureName { get; } = "pocket";

    [DataField("slotFlags")] public SlotFlags SlotFlags { get; } = SlotFlags.PREVENTEQUIP;

    [DataField("stripTime")] public float StripTime { get; } = 3f;

    [DataField("uiContainer")] public SlotUIContainer UIContainer { get; } = SlotUIContainer.None;

    [DataField("uiWindowPos", required: true)] public Vector2i UIWindowPosition { get; }

    //todo this is supreme shit and ideally slots should be stored in a given equipmentslotscomponent on each equipment
    [DataField("dependsOn")] public string? DependsOn { get; }

    [DataField("displayName", required: true)] public string DisplayName { get; } = string.Empty;

    [DataField("stripHidden")] public bool StripHidden { get; }

    /// <summary>
    ///     Offset for the clothing sprites.
    /// </summary>
    [DataField("offset")] public Vector2 Offset { get; } = Vector2.Zero;
}

public enum SlotUIContainer
{
    None,
    BottomLeft,
    BottomRight,
    Top
}
