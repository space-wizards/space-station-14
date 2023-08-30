
namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Specifies behavior when none of the jobs you want are available at round start.
    /// </summary>
    public enum PreferenceUnavailableMode
    {
        // These enum values HAVE to match the ones in DbPreferenceUnavailableMode in Server.Database.

        /// <summary>
        ///     Stay in the lobby (if the lobby is enabled).
        /// </summary>
        StayInLobby = 0,

        /// <summary>
        ///     Spawn as overflow role if preference unavailable.
        /// </summary>
        SpawnAsOverflow,
    }
}
