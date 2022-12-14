using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;

/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
[Access(typeof(HeadsetSystem))]
public sealed class HeadsetComponent : Component
{
    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public readonly HashSet<string> Channels = new() { "Common" };

    // Maybe make the defaultChannel an actual channel type some day, and use that for parsing messages
    // [DataField("defaultChannel", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    // public readonly HashSet<string> defaultChannel = new();

    [DataField("defaultChannel")]
    public string defaultChannel = string.Empty;

    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;
}
