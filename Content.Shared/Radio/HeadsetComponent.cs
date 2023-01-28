using Content.Shared.Chat;
using Content.Shared.Inventory;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
public sealed class HeadsetComponent : Component
{
    /// <summary>
    ///     Set of accessible radio channels that can be addressed by using a channel specific prefix (e.g., ":e")
    /// </summary>
    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public readonly HashSet<string> Channels = new() { SharedChatSystem.CommonChannel };

    /// <summary>
    ///     Some specific radio channel that can be addressed via the <see cref="SharedChatSystem.DefaultChannelKey"/> prefix.
    /// </summary>
    [DataField("defaultChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public readonly string? DefaultChannel;

    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    // currently non-functional due to how TryProccessRadioMessage() works.
    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;
}
