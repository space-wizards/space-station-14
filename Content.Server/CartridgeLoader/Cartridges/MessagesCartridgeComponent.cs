namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessagesCartridgeComponent : Component
{
    /// <summary>
    /// The list of messages cached by the device.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> Messages = new();

    /// <summary>
    /// The list of messages the device is trying to send.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> MessagesQueue = new();

    /// <summary>
    /// The uid of the current user
    /// </summary>
    [DataField]
    public int? UserUid = null;

     /// <summary>
    /// The uid of the crew the user is chatting with
    /// </summary>
    [DataField]
    public int? ChatUid = null;

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<string,string> nameDict= new();

    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);
    public TimeSpan NextUpdate = TimeSpan.Zero;


}
