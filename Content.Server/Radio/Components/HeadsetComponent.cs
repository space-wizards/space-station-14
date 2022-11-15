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
    //[DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    [ViewVariables]
    public HashSet<string> Channels = new()
    {
        "Common"
    };

    [DataField("keysPrototypes", required: true)]
    public List<string> KeysPrototypes = new List<string>();
    [ViewVariables]
    public List<EntityUid> KeysInstalled = new List<EntityUid>();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isKeysExtractable")]
    public bool IsKeysExtractable = true;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keySlotsAmount")]
    public int KeySlotsAmount = 2;

    [DataField("keyExtarctionSound")]
    public SoundSpecifier KeyExtarctionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");
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
