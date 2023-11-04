using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Robust.Server.Player;
using Robust.Shared.Toolshed;


namespace Content.Server.Administration.Managers
{
    /// <summary>
    ///     Manages server administrators and their permission flags.
    /// </summary>
    public interface IAdminManager : ISharedAdminManager
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

        bool TryGetCommandFlags(CommandSpec command, out AdminFlags[]? flags);
    }
}
