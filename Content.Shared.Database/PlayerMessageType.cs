namespace Content.Shared.Database;

/// <summary>
/// Message type for persistent message storage
/// Like the other enums used in databases, do not change the numbers or experience horrors of database tomfoolery
/// </summary>
public enum PlayerMessageType
{
    /// <summary>
    /// Catch-all for messages that need to be stored for unremarkable reasons.
    /// <b>You'll probably want to use or add something more specific</b>
    /// </summary>
    General = 0,
    /// <summary>
    /// Messages learned by parrots with a ParrotDbMemoryComponent
    /// </summary>
    Parrot = 1
}
