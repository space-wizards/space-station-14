using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessagesCartridgeComponent : Component
{
    /// <summary>
    /// The list of messages cached by the device.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> Messages = [];

    /// <summary>
    /// The list of messages the device is trying to send.
    /// </summary>
    [DataField]
    public List<MessagesMessageData> MessagesQueue = [];

    /// <summary>
    /// The uid of the current user
    /// </summary>
    [DataField]
    public int? UserUid = null;

    /// <summary>
    /// The name and job of the user
    /// </summary>
    [DataField]
    public string? UserName = null;

    /// <summary>
    /// The uid of the crew the user is chatting with
    /// </summary>
    [DataField]
    public int? ChatUid = null;

    /// <summary>
    /// ID card connected to the Cartridge
    /// </summary>
    [DataField]
    public EntityUid? ConnectedId = null;

    /// <summary>
    /// Dictionary translating uids to readable names
    /// </summary>
    [DataField]
    public Dictionary<int, string> NameDict = [];

    /// <summary>
    /// Key used to denote which faction the cartridge belongs to.
    /// </summary>
    [DataField]
    public MessagesKeys EncryptionKey = MessagesKeys.Nanotrasen;

}


