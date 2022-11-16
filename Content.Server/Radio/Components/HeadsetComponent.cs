using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Radio.Components;
/// <summary>
///     This component relays radio messages to the parent entity's chat when equipped.
/// </summary>
[RegisterComponent]
[Access(typeof(HeadsetSystem))]
public sealed class HeadsetComponent : Component
{
    //[DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    [ViewVariables]
    public HashSet<string> Channels = new()
    {
        "Common"
    }; //Fills only by encryption chips in it

    //[DataField("keysPrototypes", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    //public List<string> KeysPrototypes = new();
    // [ViewVariables]
    // public List<EntityUid> KeysInstalled = new List<EntityUid>();
    [ViewVariables]
    public int KeysInstalledAmount = 0;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keySlotsAmount")]
    public int KeySlotsAmount = 2;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isKeysExtractable")]
    public bool IsKeysExtractable = true;

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
