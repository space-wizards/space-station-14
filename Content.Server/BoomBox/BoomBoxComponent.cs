using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.BoomBox;


[RegisterComponent, Access(typeof(BoomBoxSystem))]
public sealed partial class BoomBoxComponent : Component
{
    // This field displays whether music is currently playing.
    public bool Enabled = false;

    // This field is needed to work with the audio stream
    public EntityUid? Stream = null;

    // This field is needed to adjust the volume of the boombox :)
    public float Volume = -13f;

    // This field shows the presence of the cassette in the boombox
    public bool Inserted = false;

    public EntityUid User;

    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    [DataField("port", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string Port = "Pressed";

    public string SoundPath = "/Audio/Stories/Objects/Devices/boombox/FanfareRussian.ogg";


    [DataField("slots")]
    public Dictionary<string, ItemSlot> Slots = new();

}
