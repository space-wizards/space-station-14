using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.Radio.Components;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessagesCartridgeComponent : Component
{
    /// <summary>
    /// The component of the last contacted server
    /// </summary>
    [DataField]
    public MessagesServerComponent? LastServer = null;

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


