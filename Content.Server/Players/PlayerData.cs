using Robust.Server.Player;
using Robust.Shared.Network;

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
        ///     This is a backup copy of the player name stored on connection.
        ///     This is useful in the event the player disconnects.
        /// </summary>
        [ViewVariables]
        public string Name { get; }

        /// <summary>
        ///     The currently occupied mind of the player owning this data.
        ///     DO NOT DIRECTLY SET THIS UNLESS YOU KNOW WHAT YOU'RE DOING.
        /// </summary>
        [ViewVariables]
        public Mind.Mind? Mind { get; private set; }

        /// <summary>
        ///     If true, the player is an admin and they explicitly de-adminned mid-game,
        ///     so they should not regain admin if they reconnect.
        /// </summary>
        public bool ExplicitlyDeadminned { get; set; }

        public void WipeMind()
        {
            Mind?.TransferTo(null);
            // This will ensure Mind == null
            Mind?.ChangeOwningPlayer(null);
        }

        /// <summary>
        /// Called from Mind.ChangeOwningPlayer *and nowhere else.*
        /// </summary>
        public void UpdateMindFromMindChangeOwningPlayer(Mind.Mind? mind)
        {
            Mind = mind;
        }

        public PlayerData(NetUserId userId, string name)
        {
            UserId = userId;
            Name = name;
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
