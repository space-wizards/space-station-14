using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Commands;

public static class CommandChecks
{
    public static bool MustNotBeServer(IConsoleShell shell, [NotNullWhen(true)] out ICommonSession? player)
    {
        player = shell.Player;

        if (player is not { Status: SessionStatus.InGame })
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));

        return shell.Player != null;
    }

    public static bool MustBeAttachedToEntity(IConsoleShell shell,
        [NotNullWhen(true)] out ICommonSession? player,
        [NotNullWhen(true)] out EntityUid? entity)
    {
        MustNotBeServer(shell, out player);
        entity = player?.AttachedEntity;

        if (player != null && entity == null)
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));

        return entity != null;
    }

    public static bool NeedExactlyZeroArguments(IConsoleShell shell, string[] args)
    {
        if (args.Length > 0)
            shell.WriteError(Loc.GetString("shell-need-exactly-zero-arguments"));

        return args.Length == 0;
    }

    public static bool NeedExactlyOneArgument(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));

        return args.Length == 1;
    }
}
