using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Players;

namespace Content.Server.Players
{
    public static class PlayerDataExt
    {
        /// <summary>
        ///     Gets the correctly cast instance of content player data from an engine player data storage.
        /// </summary>
        public static PlayerData? ContentData(this IPlayerSession session)
        {
            return session.Data.ContentData();
        }

        public static PlayerData? ContentData(this ICommonSession session)
        {
            return ((IPlayerSession) session).ContentData();
        }

        /// <summary>
        ///     Gets the mind that is associated with this player.
        /// </summary>
        public static EntityUid? GetMind(this IPlayerSession session)
        {
            return session.Data.ContentData()?.Mind;
        }
    }
}
