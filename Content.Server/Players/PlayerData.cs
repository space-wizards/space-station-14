using Content.Server.Mobs;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Network;

namespace Content.Server.Players
{
    /// <summary>
    ///     Content side for all data that tracks a player session.
    ///     Use <see cref="PlayerDataExt.ContentData(IPlayerData)"/> to retrieve this from an <see cref="IPlayerData"/>.
    /// </summary>
    public sealed class PlayerData
    {
        /// <summary>
        ///     The session ID of the player owning this data.
        /// </summary>
        public NetSessionId SessionId { get; }

        /// <summary>
        ///     The currently occupied mind of the player owning this data.
        /// </summary>
        public Mind Mind { get; set; }

        public PlayerData(NetSessionId sessionId)
        {
            SessionId = sessionId;
        }
    }

    public static class PlayerDataExt
    {
        /// <summary>
        ///     Gets the correctly cast instance of content player data from an engine player data storage.
        /// </summary>
        public static PlayerData ContentData(this IPlayerData data)
        {
            return (PlayerData)data.ContentDataUncast;
        }

        /// <summary>
        ///     Gets the correctly cast instance of content player data from an engine player data storage.
        /// </summary>
        public static PlayerData ContentData(this IPlayerSession session)
        {
            return session.Data.ContentData();
        }
    }
}
