using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.Radio.Components;

/// <summary>
/// Entities with <see cref="MessagesServerComponent"/> are needed to transmit messages using PDAs.
/// They also need to be powered by <see cref="ApcPowerReceiverComponent"/>
/// in order for them to work on the same map as server.
/// </summary>
[RegisterComponent]
public sealed partial class MessagesServerComponent : Component
{
    /// <summary>
    /// The list of messages cached by the server.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> Messages = new();

    /// <summary>
    /// The list of messages the server is trying to send.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> MessagesQueue = new();

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<string,string> NameDict= new();

    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
