using Robust.Shared.Serialization;

namespace Content.Shared.Item.ItemToggle.Components;

[RegisterComponent]
public sealed partial class ItemToggleVisualsComponent : Component
{
    [DataField]
    public string? HeldPrefixOn = "on";

    [DataField]
    public string? HeldPrefixOff = "off";

    [DataField]
    public string? EquippedPrefixOn = "on";

    [DataField]
    public string? EquippedPrefixOff = "off";
}

[Serializable, NetSerializable]
public enum ItemToggleVisuals
{
    State,
    Layer,
}
