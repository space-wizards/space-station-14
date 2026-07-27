using Content.Shared.Chat;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is currently used for providing access to channels for "HeadsetComponent"s.
///     It should be used for intercoms and other radios in future.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EncryptionKeyComponent : Component
{
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    ///     This is the channel that will be used when using the default/department prefix (<see cref="SharedChatSystem.DefaultChannelKey"/>).
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>? DefaultChannel;
}
