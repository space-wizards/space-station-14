using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Radio.Components;
/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
[Access(typeof(HeadsetSystem))]
public sealed class HeadsetComponent : Component
{
    [ViewVariables]
    public HashSet<string> Channels = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keySlotsAmount")]
    public int KeySlotsAmount = 2;

    /*
     * IsKeysExtractable == true  => Human will able to screw out encryption keys with screwdriver (for KeysExtractionMethod == "Screwing")
     * IsKeysExtractable == false => encryption keys will be locked in headset, there will be no proper way to extract them.
    */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isKeysExtractable")]
    public bool IsKeysExtractable = true;
    // Shows what tool human should use to extract encryption keys from headset
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keysExtractionMethod")]
    public string KeysExtractionMethod = "Screwing";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyExtractionSound")]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyInsertionSound")]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container KeyContainer = default!;
    public const string KeyContainerName = "key_slots";

    [DataField("enabled")]
    public bool Enabled = true;

    public bool IsEquipped = false;

    [DataField("requiredSlot")]
    public SlotFlags RequiredSlot = SlotFlags.EARS;
}
