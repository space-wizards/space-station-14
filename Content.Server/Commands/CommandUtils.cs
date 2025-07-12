using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Commands;

/// <summary>
/// Utilities for writing commands
/// </summary>
public static class CommandUtils
{
    /// <summary>
    /// Gets the player session for the player with the indicated id,
    /// sending a failure to the performer if unable to.
    /// </summary>
    public static bool TryGetSessionByUsernameOrId(IConsoleShell shell,
        string usernameOrId,
        ISharedPlayerManager? playerManager,
        [NotNullWhen(true)] out ICommonSession? session)
    {
        IoCManager.Resolve(ref playerManager);
        if (playerManager.TryGetSessionByUsername(usernameOrId, out session))
            return true;

        if (Guid.TryParse(usernameOrId, out var targetGuid) && playerManager.TryGetSessionById(new NetUserId(targetGuid), out session))
            return true;

        shell.WriteLine("Unable to find user with that name/id.");
        return false;
    }

    /// <summary>
    /// Gets the attached entity for the player session with the indicated id,
    /// sending a failure to the performer if unable to.
    /// </summary>
    public static bool TryGetAttachedEntityByUsernameOrId(IConsoleShell shell,
        string usernameOrId,
        ISharedPlayerManager? playerManager,
        out EntityUid attachedEntity)
    {
        attachedEntity = default;
        if (!TryGetSessionByUsernameOrId(shell, usernameOrId, playerManager, out var session))
            return false;

        if (session.AttachedEntity == null)
        {
            shell.WriteError("User has no attached entity.");
            return false;
        }

        attachedEntity = session.AttachedEntity.Value;
        return true;
    }
}
