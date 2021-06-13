using Content.Shared.GameWindow;
using Robust.Server.Player;

namespace Content.Server.Players
{
    public static class PlayerSessionExt
    {
        public static void RequestWindowAttention(this IPlayerSession session)
        {
            var msg = session.ConnectedClient.CreateNetMessage<MsgRequestWindowAttention>();
            session.ConnectedClient.SendMessage(msg);
        }
    }
}
