using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


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
    public List<MessagesMessageData> Messages = [];

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<int, string> NameDict = [];

    ///<summary>
    /// Delay between updates on the given server.
    ///</summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(10);

    ///<summary>
    /// The next time the server will be updated
    ///</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
