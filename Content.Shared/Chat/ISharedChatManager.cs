namespace Content.Shared.Chat;

public interface ISharedChatManager
{
    void Initialize();

    /// <summary>
    /// Send an admin alert to the admin chat channel.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void SendAdminAlert(string message);

    /// <summary>
    /// Send an admin alert to the admin chat channel specifically about the given player.
    /// Will include info extra like their antag status and name.
    /// </summary>
    /// <param name="player">The player that the message is about.</param>
    /// <param name="message">The message to send.</param>
    void SendAdminAlert(EntityUid player, string message);

    /// <summary>
    /// This is a dangerous function! Only pass in property escaped text.
    /// See: <see cref="SendAdminAlert(string)"/>
    /// <br/><br/>
    /// Use this for things that need to be unformatted (like tpto links) but ensure that everything else
    /// is formated properly. If it's not, players could sneak in ban links or other nasty commands that the admins
    /// could clink on.
    /// </summary>
    void SendAdminAlertNoFormatOrEscape(string message);
}
