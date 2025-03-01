namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannelFilter channel)
    {
        return channel switch
        {
            ChatChannelFilter.Server => Color.Orange,
            ChatChannelFilter.Radio => Color.LimeGreen,
            ChatChannelFilter.LOOC => Color.MediumTurquoise,
            ChatChannelFilter.OOC => Color.LightSkyBlue,
            ChatChannelFilter.Dead => Color.MediumPurple,
            ChatChannelFilter.Admin => Color.Red,
            ChatChannelFilter.AdminAlert => Color.Red,
            ChatChannelFilter.AdminChat => Color.HotPink,
            ChatChannelFilter.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }
}
