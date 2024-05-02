using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessagesCartridgeComponent : Component
{
    /// <summary>
    /// The list of messages the device is trying to send.
    /// </summary>
    [DataField]
    public Stack<MessagesMessageData> MessagesQueue = [];

    /// <summary>
    /// The message system user id of the crew the user is chatting with
    /// </summary>
    [DataField]
    public int? ChatUid = null;

    /// <summary>
    /// Key used to denote which faction the cartridge belongs to.
    /// </summary>
    [DataField]
    public MessagesKeys EncryptionKey = MessagesKeys.Nanotrasen;
}


