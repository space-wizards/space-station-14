using Content.Shared.Inventory;
using Robust.Shared.Audio;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
public sealed partial class HeadsetComponent : Component
{
    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;

    // DS14-start 
    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    [DataField]
    public SoundSpecifier RadioReceiveSoundPath = new SoundPathSpecifier("/Audio/_DeadSpace/Items/Misc/radio_headset_receive.ogg");
    // DS14-end 
}
