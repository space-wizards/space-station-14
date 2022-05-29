using Content.Shared.Administration;
using Robust.Server.Player;


namespace Content.Server.Administration.Managers
{
    /// <summary>
    ///     Manages server administrators and their permission flags.
    /// </summary>
    public interface IAdminManager
    {
        /// <summary>
        ///     Fired when the permissions of an admin on the server changed.
        /// </summary>
        event Action<AdminPermsChangedEventArgs> OnPermsChanged;

        /// <summary>
        ///     Gets all active admins currently on the server.
        /// </summary>
        /// <remarks>
        ///     This does not include admins that are de-adminned.
        /// </remarks>
        IEnumerable<IPlayerSession> ActiveAdmins { get; }

        /// <summary>
        /// Gets all admins currently on the server, even de-adminned ones.
        /// </summary>
        IEnumerable<IPlayerSession> AllAdmins { get; }

        /// <summary>
        ///     Checks if a player is an admin.
        /// </summary>
        /// <param name="session">The player to check.</param>
        /// <param name="includeDeAdmin">
        ///     Whether to return admin data for admins that are current de-adminned.
        /// </param>
        /// <returns>true if the player is an admin, false otherwise.</returns>
        bool IsAdmin(IPlayerSession session, bool includeDeAdmin = false);

        /// <summary>
        ///     Gets the admin data for a player, if they are an admin.
        /// </summary>
        /// <param name="session">The player to get admin data for.</param>
        /// <param name="includeDeAdmin">
        ///     Whether to return admin data for admins that are current de-adminned.
        /// </param>
        /// <returns><see langword="null" /> if the player is not an admin.</returns>
        AdminData? GetAdminData(IPlayerSession session, bool includeDeAdmin = false);

        /// <summary>
        ///     See if a player has an admin flag.
        /// </summary>
        /// <returns>True if the player is and admin and has the specified flags.</returns>
        bool HasAdminFlag(IPlayerSession player, AdminFlags flag)
        {
            var data = GetAdminData(player);
            return data != null && data.HasFlag(flag);
        }

        /// <summary>
        ///     De-admins an admin temporarily so they are effectively a normal player.
        /// </summary>
        /// <remarks>
        ///     De-adminned admins are able to re-admin at any time if they so desire.
        /// </remarks>
        void DeAdmin(IPlayerSession session);

        /// <summary>
        ///     Re-admins a de-adminned admin.
        /// </summary>
        void ReAdmin(IPlayerSession session);

        /// <summary>
        ///     Re-loads the permissions of an player in case their admin data changed DB-side.
        /// </summary>
        /// <seealso cref="ReloadAdminsWithRank"/>
        void ReloadAdmin(IPlayerSession player);

        /// <summary>
        ///     Reloads admin permissions for all admins with a certain rank.
        /// </summary>
        /// <param name="rankId">The database ID of the rank.</param>
        /// <seealso cref="ReloadAdmin"/>
        void ReloadAdminsWithRank(int rankId);

        void Initialize();

        void PromoteHost(IPlayerSession player);
    }
}
