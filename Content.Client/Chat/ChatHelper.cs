using Content.Shared.Chat;
using Robust.Shared.Maths;

namespace Content.Client.Chat
{
    public class ChatHelper
    {
        public static Color ChatColor(ChatChannel channel) =>
            channel switch
            {
                ChatChannel.Server => Color.Orange,
                ChatChannel.Radio => Color.LimeGreen,
                ChatChannel.LOOC => Color.LightSkyBlue,
                ChatChannel.OOC => Color.RoyalBlue,
                ChatChannel.Dead => Color.MediumPurple,
                ChatChannel.Admin => Color.Red,
                ChatChannel.Whisper => Color.DarkGray,
                _ => Color.LightGray
            };
    }
}
