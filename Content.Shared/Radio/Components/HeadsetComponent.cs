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

    /// <summary>
    /// The sound effect played when headset receive message
    /// </summary>
    [DataField]
    public SoundSpecifier MessageReceiveSound = new SoundPathSpecifier("/Audio/Effects/radio_receive.ogg");

    /// <summary>
    /// The sound effect played when headset send message
    /// </summary>
    [DataField]
    public SoundSpecifier MessageSendSound = new SoundPathSpecifier("/Audio/Effects/radio_talk.ogg");
}
