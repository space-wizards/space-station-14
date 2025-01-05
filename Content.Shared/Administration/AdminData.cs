
namespace Content.Shared.Administration
{
    /// <summary>
    ///     Represents data for a single server admin.
    /// </summary>
    public sealed class AdminData
    {
        // Can be false if they're de-adminned with the ability to re-admin.
        /// <summary>
        ///     Whether the admin is currently active. This can be false if they have de-adminned mid-round.
        /// </summary>
        public bool Active;

        /// <summary>
        /// Whether the admin is in stealth mode and won't appear in adminwho to admins without the Stealth flag.
        /// </summary>
        public bool Stealth;

        /// <summary>
        ///     The admin's title.
        /// </summary>
        public string? Title;

        /// <summary>
        ///     The admin's permission flags.
        /// </summary>
        public AdminFlags Flags;

        /// <summary>
        ///     Checks whether this admin has an admin flag.
        /// </summary>
        /// <param name="flag">The flags to check. Multiple flags can be specified, they must all be held.</param>
        /// <param name="includeDeAdmin">If true then also count flags even if the admin has de-adminned.</param>
        /// <returns>False if this admin is not <see cref="Active"/> or does not have all the flags specified.</returns>
        public bool HasFlag(AdminFlags flag, bool includeDeAdmin = false)
        {
            return (includeDeAdmin || Active) && (Flags & flag) == flag;
        }

        /// <summary>
        ///     Check if this admin can spawn stuff in with the entity/tile spawn panel.
        /// </summary>
        public bool CanAdminPlace()
        {
            return HasFlag(AdminFlags.Spawn);
        }

        /// <summary>
        ///     Check if this admin can execute server-side C# scripts.
        /// </summary>
        public bool CanScript()
        {
            return HasFlag(AdminFlags.Host);
        }

        /// <summary>
        ///     Check if this admin can open the admin menu.
        /// </summary>
        public bool CanAdminMenu()
        {
            return HasFlag(AdminFlags.Admin);
        }

        /// <summary>
        /// Check if this admin can be hidden and see other hidden admins.
        /// </summary>
        public bool CanStealth()
        {
            return HasFlag(AdminFlags.Stealth);
        }

        public bool CanAdminReloadPrototypes()
        {
            return HasFlag(AdminFlags.Host);
        }
    }
}
