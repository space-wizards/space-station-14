using Content.Shared.Inventory;

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
    /// Who is this headset currently worn by?
    /// </summary>
    public EntityUid? CurrentlyWornBy;

    /// <summary>
    /// What channels does this headset use? This is determined by its encryption keys, so isn't set via YAML.
    /// </summary>
    public HashSet<string> ChannelNames = new();
}
