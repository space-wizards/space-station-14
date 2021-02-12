using System;

namespace Content.Shared.Administration
{
    /// <summary>
    ///     Permissions that admins can have.
    /// </summary>
    [Flags]
    public enum AdminFlags : uint
    {
        None = 0,

        /// <summary>
        ///     Basic admin verbs.
        /// </summary>
        Admin = 1 << 0,

        /// <summary>
        ///     Ability to ban people.
        /// </summary>
        Ban = 1 << 1,

        /// <summary>
        ///     Debug commands for coders.
        /// </summary>
        Debug = 1 << 2,

        /// <summary>
        ///     !!FUN!!
        /// </summary>
        Fun = 1 << 3,

        /// <summary>
        ///     Ability to edit permissions for other administrators.
        /// </summary>
        Permissions = 1 << 4,

        /// <summary>
        ///     Ability to control teh server like restart it or change the round type.
        /// </summary>
        Server = 1 << 5,

        /// <summary>
        ///     Ability to spawn stuff in.
        /// </summary>
        Spawn = 1 << 6,

        /// <summary>
        ///     Ability to use VV.
        /// </summary>
        VarEdit = 1 << 7,

        /// <summary>
        ///     Large mapping operations.
        /// </summary>
        Mapping = 1 << 8,

        /// <summary>
        ///     Makes you british.
        /// </summary>
        //Piss = 1 << 9,

        /// <summary>
        ///     Dangerous host permissions like scsi.
        /// </summary>
        Host = 1u << 31,
    }
}
