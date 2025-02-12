using Content.Shared.Inventory;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
public sealed partial class HeadsetComponent : Component
{
    [DataField]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField]
    public SlotFlags RequiredSlot = SlotFlags.EARS;
}
