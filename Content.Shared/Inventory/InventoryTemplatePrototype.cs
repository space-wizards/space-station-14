using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

[Prototype("inventoryTemplate")]
public sealed partial class InventoryTemplatePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("slots")] public SlotDefinition[] Slots { get; private set; } = Array.Empty<SlotDefinition>();
}

[DataDefinition]
public sealed partial class SlotDefinition
{
    [DataField("name", required: true)] public string Name { get; private set; } = string.Empty;
    [DataField("slotTexture")] public string TextureName { get; private set; } = "pocket";
    [DataField("slotFlags")] public SlotFlags SlotFlags { get; private set; } = SlotFlags.PREVENTEQUIP;
    [DataField("showInWindow")] public bool ShowInWindow { get; private set; } = true;
    [DataField("slotGroup")] public string SlotGroup { get; private set; } = "Default";
    [DataField("stripTime")] public TimeSpan StripTime { get; private set; } = TimeSpan.FromSeconds(4f);

    [DataField("uiWindowPos", required: true)]
    public Vector2i UIWindowPosition { get; private set; }

    [DataField("strippingWindowPos", required: true)]
    public Vector2i StrippingWindowPos { get; private set; }

    [DataField("dependsOn")] public string? DependsOn { get; private set; }

    [DataField("displayName", required: true)]
    public string DisplayName { get; private set; } = string.Empty;

    [DataField("stripHidden")] public bool StripHidden { get; private set; }

    /// <summary>
    ///     Offset for the clothing sprites.
    /// </summary>
    [DataField("offset")] public Vector2 Offset { get; private set; } = Vector2.Zero;

    /// <summary>
    ///     Entity whitelist for CanEquip checks.
    /// </summary>
    [DataField("whitelist")] public EntityWhitelist? Whitelist = null;

    /// <summary>
    ///     Entity blacklist for CanEquip checks.
    /// </summary>
    [DataField("blacklist")] public EntityWhitelist? Blacklist = null;
}
