using Content.Shared.CartridgeLoader.Cartridges;

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
    public string? UserUid = null;

    /// <summary>
    /// The name and job of the user
    /// </summary>
    [DataField]
    public string? UserName = null;

     /// <summary>
    /// The uid of the crew the user is chatting with
    /// </summary>
    [DataField]
    public string? ChatUid = null;

    /// <summary>
    /// ID card connected to the Cartridge
    /// </summary>
    [DataField]
    public EntityUid? ConnectedId = null;

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<string,string> NameDict= new();

    /// <summary>
    /// Whether the cartridge has lost connection and should be looking for a new server
    /// </summary>
    [DataField]
    public bool DeadConnection=true;

    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(10);
    public TimeSpan NextUpdate = TimeSpan.Zero;


}
