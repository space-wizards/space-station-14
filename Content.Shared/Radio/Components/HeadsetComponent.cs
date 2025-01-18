using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

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
    /// Sound of receiving message from toggled channel.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound;

    /// <summary>
    /// Headset will emit sound when receives message from these channels.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> ToggledSoundChannels = [];
}
