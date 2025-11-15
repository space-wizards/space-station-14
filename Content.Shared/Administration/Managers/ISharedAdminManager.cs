using Robust.Shared.Player;

namespace Content.Shared.Administration.Managers;

/// <summary>
///     Manages server administrators and their permission flags.
/// </summary>
public interface ISharedAdminManager
{
    void Initialize();

    /// <summary>
    ///     Gets the admin data for a player, if they are an admin.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own entity.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to return admin data for admins that are current de-adminned.
    /// </param>
    /// <returns><see langword="null" /> if the player is not an admin.</returns>
    AdminData? GetAdminData(EntityUid uid, bool includeDeAdmin = false);

    /// <summary>
    ///     Gets the admin data for a player, if they are an admin.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own session.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to return admin data for admins that are current de-adminned.
    /// </param>
    /// <returns><see langword="null" /> if the player is not an admin.</returns>
    AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false);

    /// <summary>
    ///     See if a player has an admin flag.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own entity.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to check flags even for admins that are current de-adminned.
    /// </param>
    /// <returns>True if the player is and admin and has the specified flags.</returns>
    bool HasAdminFlag(EntityUid player, AdminFlags flag, bool includeDeAdmin = false);

    /// <summary>
    ///     See if a player has an admin flag.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own session.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to check flags even for admins that are current de-adminned.
    /// </param>
    /// <returns>True if the player is and admin and has the specified flags.</returns>
    bool HasAdminFlag(ICommonSession player, AdminFlags flag, bool includeDeAdmin = false);

    /// <summary>
    ///     Checks if a player is an admin.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own entity.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to return admin data for admins that are current de-adminned.
    /// </param>
    /// <returns>true if the player is an admin, false otherwise.</returns>
    bool IsAdmin(EntityUid uid, bool includeDeAdmin = false);

    /// <summary>
    ///     Checks if a player is an admin.
    /// </summary>
    /// <remarks>
    ///     When used by the client, this only returns accurate results for the player's own session.
    /// </remarks>
    /// <param name="includeDeAdmin">
    ///     Whether to return admin data for admins that are current de-adminned.
    /// </param>
    /// <returns>true if the player is an admin, false otherwise.</returns>
    bool IsAdmin(ICommonSession session, bool includeDeAdmin = false);

    void ReloadCommandPermissions();
    void ReloadToolshedPermissions();
}
