using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can innately send messages on the radio.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanRadioComponent : Component
{
    /// <summary>
    /// What channels can this entity talk on?
    /// </summary>
    [DataField("sendChannels"), AutoNetworkedField]
    public HashSet<string> SendChannels = new();

    /// <summary>
    /// What channels can this entity receive on?
    /// </summary>
    [DataField("receiveChannels"), AutoNetworkedField]
    public HashSet<string> ReceiveChannels = new();

    [DataField("receiveAllChannels"), AutoNetworkedField]
    public bool CanListenOnAllChannels;

    [DataField("globalReceive"), AutoNetworkedField]
    public bool IsInfiniteRange;
}
