namespace Content.Client.Storage.Components;

[RegisterComponent]
public sealed partial class ItemSlotVisualsComponent : Component
{
    [DataField]
    public int MaxFillLevels;

    [DataField]
    public string? FillBaseName;

    [DataField]
    public string? InHandsFillBaseName;

    [DataField]
    public int InHandsMaxFillLevels;

    [DataField]
    public string? EquippedFillBaseName;

    [DataField]
    public int EquippedMaxFillLevels;

    [DataField("layer")]
    public ItemSlotVisualLayers FillLayer = ItemSlotVisualLayers.Fill;
}

public enum ItemSlotVisualLayers : byte
{
    ContainsItem,
    Fill
}
