using Content.Shared.Administration;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight;

/// <summary>
///     Represents data for a single player.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerData
{
    /// <summary>
    ///     The player's title.
    /// </summary>
    public string? Title;
    
    public string? GhostTheme;
    
    public int Balance;

    /// <summary>
    ///     The player's permission flags.
    /// </summary>
    public PlayerFlags Flags;

    /// <summary>
    ///     Checks whether this admin has an admin flag.
    /// </summary>
    /// <param name="flag">The flags to check. Multiple flags can be specified, they must all be held.</param>
    /// <returns>False if this admin is not <see cref="Active"/> or does not have all the flags specified.</returns>
    public bool HasFlag(PlayerFlags flag)
    {
        return (Flags & flag) == flag;
    }
}
[Flags]
public enum PlayerFlags : uint
{
    None = 0,

    AlfaTester = 1 << 0,
    BetaTester = 1 << 1,

    Staff = 1 << 2,
    Mentor = 1 << 3,
    Retiree = 1 << 4,

    Patron1 = 1 << 5,
    Patron2 = 1 << 6,
    Patron3 = 1 << 7,
    Patron4 = 1 << 8,
    Patron5 = 1 << 9,
    Patron6 = 1 << 10,

    GoldEventWinner = 1 << 11,
    SilverEventWinner = 1 << 12,
    CopperEventWinner = 1 << 13,

    AllRoles = 1 << 14,
}
