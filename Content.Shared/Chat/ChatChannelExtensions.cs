namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.Orange,
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.MediumTurquoise,
            ChatChannel.OOC => Color.LightSkyBlue,
            ChatChannel.Dead => Color.MediumPurple,
            ChatChannel.Admin => Color.Red,
            ChatChannel.AdminAlert => Color.Red,
            ChatChannel.AdminChat => Color.HotPink,
            ChatChannel.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }

    // Starlight
    public static bool IsExcemptFromLanguages(this ChatChannel channel) =>
    channel is ChatChannel.LOOC or ChatChannel.Emotes or ChatChannel.Notifications or ChatChannel.Visual;
}
