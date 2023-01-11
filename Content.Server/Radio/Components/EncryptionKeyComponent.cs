using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;
/// <summary>
///     This component currently used for providing access to channels for "HeadsetComponent"s.
///     It should be used for intercoms and other radios in future.
/// </summary>
[RegisterComponent]
public sealed class EncryptionKeyComponent : Component
{
    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new();
}
