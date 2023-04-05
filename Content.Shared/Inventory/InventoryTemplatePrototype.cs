using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

[Prototype("inventoryTemplate")]
public sealed class InventoryTemplatePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("slots")] public SlotDefinition[] Slots { get; } = Array.Empty<SlotDefinition>();
}

[DataDefinition]
public sealed class SlotDefinition
{
    [DataField("name", required: true)] public string Name { get; } = string.Empty;
    [DataField("slotTexture")] public string TextureName { get; } = "pocket";
    [DataField("slotFlags")] public SlotFlags SlotFlags { get; } = SlotFlags.PREVENTEQUIP;
    [DataField("showInWindow")] public bool ShowInWindow { get; } = true;
    [DataField("slotGroup")] public string SlotGroup { get; } = "Default";
    [DataField("stripTime")] public float StripTime { get; } = 4f;

    [DataField("uiWindowPos", required: true)]
    public Vector2i UIWindowPosition { get; }

    [DataField("strippingWindowPos", required: true)]
    public Vector2i StrippingWindowPos { get; }

    [DataField("dependsOn")] public string? DependsOn { get; }

    [DataField("displayName", required: true)]
    public string DisplayName { get; } = string.Empty;

    [DataField("stripHidden")] public bool StripHidden { get; }

    /// <summary>
    ///     Offset for the clothing sprites.
    /// </summary>
    [DataField("offset")] public Vector2 Offset { get; } = Vector2.Zero;

    /// <summary>
    ///     Entity whitelist for CanEquip checks.
    /// </summary>
    [DataField("whitelist")] public EntityWhitelist? Whitelist = null;

    /// <summary>
    ///     Entity blacklist for CanEquip checks.
    /// </summary>
    [DataField("blacklist")] public EntityWhitelist? Blacklist = null;
}
