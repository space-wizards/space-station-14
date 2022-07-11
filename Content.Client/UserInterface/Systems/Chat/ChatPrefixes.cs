using Content.Shared.Chat;

namespace Content.Client.UserInterface.Systems.Chat;

[Flags]
public enum ChatPrefixes : ushort
{
    None = 0,

    /// <summary>
    ///     Chat heard by players within earshot
    /// </summary>
    Local = '.',

    /// <summary>
    ///     Chat heard by players right next to each other
    /// </summary>
    Whisper = ',',

    /// <summary>
    ///     Radio messages
    /// </summary>
    Radio = ';',

    /// <summary>
    ///     Local out-of-character channel
    /// </summary>
    LOOC = '(',

    /// <summary>
    ///     Out-of-character channel
    /// </summary>
    OOC = '[',

    /// <summary>
    ///     Emotes
    /// </summary>
    Emotes = '@',

    /// <summary>
    ///     Deadchat
    /// </summary>
    Dead = '\\',

    /// <summary>
    ///     Admin chat
    /// </summary>
    Admin = ']',

    Console = '/'
}
