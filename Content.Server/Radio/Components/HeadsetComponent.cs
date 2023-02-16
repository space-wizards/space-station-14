using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Radio.Components;
/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
[Access(typeof(HeadsetSystem))]
public sealed class HeadsetComponent : Component
{
    /// <summary>
    ///     This variable indicates locked state of encryption keys, allowing or prohibiting inserting and removing of encryption keys from headset.
    ///     true  => User are able to remove encryption keys with tool mentioned in KeysExtractionMethod, and put encryption keys in headset.
    ///     false => encryption keys are locked in headset, they can't be properly removed or added.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isKeysUnlocked")]
    public bool IsKeysUnlocked = true;
    /// <summary>
    ///     Shows which tool a person should use to extract the encryption keys from the headset.
    ///     default "Screwing"
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keysExtractionMethod", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string KeysExtractionMethod = "Screwing";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keySlots")]
    public int KeySlots = 2;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyExtractionSound")]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyInsertionSound")]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container KeyContainer = default!;
    public const string KeyContainerName = "key_slots";

    [ViewVariables]
    public HashSet<string> Channels = new();

    // Maybe make the defaultChannel an actual channel type some day, and use that for parsing messages
    // [DataField("defaultChannel", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    // public readonly HashSet<string> defaultChannel = new();

    /// <summary>
    ///     This variable defines what channel will be used with using ":h" (department channel prefix).
    ///     Headset read DefaultChannel of first encryption key installed.
    ///     Do not change this variable from headset or VV, better change encryption keys and UpdateDefaultChannel.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? DefaultChannel;

    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;
}
