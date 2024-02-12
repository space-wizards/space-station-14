using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can talk on a radio channel.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RadioableComponent : Component
{
    /// <summary>
    /// What channels can this entity talk on?
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Channels = new();

    [DataField("receiveAllChannels"), AutoNetworkedField]
    public bool CanListenOnAllChannels;

    [DataField("globalReceive"), AutoNetworkedField]
    public bool IsInfiniteRange;
}
