using Content.Server.Radio.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.BoomBox;


[RegisterComponent]
public sealed partial class BoomBoxTapeComponent : Component
{

    [DataField("soundPath")]
    public String SoundPath;

    [DataField("syndStatus")]
    public bool SyndStatus = false;

    [DataField("syndItem")]
    public String SyndItem;

    public bool Used = false;

}
