#nullable enable
using Content.Server.Mobs;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

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
        [ViewVariables]
        public NetUserId UserId { get; }

        /// <summary>
        ///     The currently occupied mind of the player owning this data.
        ///     DO NOT DIRECTLY SET THIS UNLESS YOU KNOW WHAT YOU'RE DOING.
        /// </summary>
        [ViewVariables]
        public Mind? Mind { get; set; }

        /// <summary>
        ///     If true, the player is an admin and they explicitly de-adminned mid-game,
        ///     so they should not regain admin if they reconnect.
        /// </summary>
        public bool ExplicitlyDeadminned { get; set; }

        public void WipeMind()
        {
            Mind?.TransferTo(null);
            Mind?.RemoveOwningPlayer();
            Mind = null;
        }

        public PlayerData(NetUserId userId)
        {
            UserId = userId;
        }
    }

    public static class PlayerDataExt
    {
        /// <summary>
        ///     Gets the correctly cast instance of content player data from an engine player data storage.
        /// </summary>
        public static PlayerData? ContentData(this IPlayerData data)
        {
            return (PlayerData?) data.ContentDataUncast;
        }

        /// <summary>
        ///     Gets the correctly cast instance of content player data from an engine player data storage.
        /// </summary>
        public static PlayerData? ContentData(this IPlayerSession session)
        {
            return session.Data.ContentData();
        }
    }
}
