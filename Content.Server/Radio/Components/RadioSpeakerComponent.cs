using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat.V2;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;

/// <summary>
/// A component that listens for chatter on a radio channel and speaks it in local chat. This needs both
/// InternalRadio (to be detected on transmission) and LocalChattableComponent (to talk) to work.
/// </summary>
[RegisterComponent]
[Access(typeof(RadioDevicesSystem))]
public sealed partial class RadioSpeakerComponent : Component
{
    /// <summary>
    /// Whether or not interacting with this entity
    /// toggles it on or off.
    /// </summary>
    [DataField("toggleOnInteract")]
    public bool ToggleOnInteract = true;

    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new () { SharedChatSystem.CommonChannel };

    [DataField("enabled")]
    public bool Enabled;
}
