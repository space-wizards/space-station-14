using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Server.Radio.Components;

/// <summary>
/// Entities with <see cref="MessagesStorageComponent"/> are needed to transmit messages using PDAs.
/// They store the messages transmitted in the system and the names of users.
/// </summary>
[RegisterComponent]
public sealed partial class MessagesStorageComponent : Component
{
    /// <summary>
    /// The list of messages cached by the server.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> Messages = [];

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<int, string> NameDict = [];

    [DataField]
    public uint ServerFrequency;
}
