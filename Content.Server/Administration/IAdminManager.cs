using System.Collections.Generic;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;

#nullable enable

namespace Content.Server.Administration
{
    /// <summary>
    ///     Manages server administrators and their permission flags.
    /// </summary>
    public interface IAdminManager
    {
        /// <summary>
        ///     Gets all active admins currently on the server.
        /// </summary>
        /// <remarks>
        ///     This does not include admins that are de-adminned.
        /// </remarks>
        IEnumerable<IPlayerSession> ActiveAdmins { get; }

        /// <summary>
        ///     Gets the admin data for a player, if they are an admin.
        /// </summary>
        /// <param name="session">The player to get admin data for.</param>
        /// <param name="includeDeAdmin">
        /// Whether to return admin data for admins that are current de-adminned.
        /// </param>
        /// <returns><see langword="null" /> if the player is not an admin.</returns>
        AdminData? GetAdminData(IPlayerSession session, bool includeDeAdmin = false);

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

        void Initialize();
    }
}
