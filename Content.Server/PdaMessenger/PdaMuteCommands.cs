using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.PdaMessenger;

/// <summary>
///     Blocks a player from sending PDA messages for the rest of the round.
/// </summary>
[AdminCommand(AdminFlags.Moderator)]
public sealed partial class PdaMuteCommand : PdaMuteCommandBase
{
    public override string Command => "pdamute";

    protected override bool Muted => true;
    protected override LocId SuccessMessage => "cmd-pdamute-success";
    protected override LocId UnchangedMessage => "cmd-pdamute-unchanged";
}

/// <summary>
///     Lets a player muted with <see cref="PdaMuteCommand"/> send PDA messages again.
/// </summary>
[AdminCommand(AdminFlags.Moderator)]
public sealed partial class PdaUnmuteCommand : PdaMuteCommandBase
{
    public override string Command => "pdaunmute";

    protected override bool Muted => false;
    protected override LocId SuccessMessage => "cmd-pdaunmute-success";
    protected override LocId UnchangedMessage => "cmd-pdaunmute-unchanged";
}

public abstract partial class PdaMuteCommandBase : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private PdaMessengerCartridgeSystem _messenger = default!;
    [Dependency] private IPlayerManager _players = default!;

    protected abstract bool Muted { get; }
    protected abstract LocId SuccessMessage { get; }
    protected abstract LocId UnchangedMessage { get; }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var target))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (!_messenger.SetMuted(target.UserId, Muted))
        {
            shell.WriteLine(Loc.GetString(UnchangedMessage,("player", target.Name)));
            return;
        }

        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{shell.Player?.Name ?? "Console"} used {Command} on {target.Name}");
        shell.WriteLine(Loc.GetString(SuccessMessage,("player", target.Name)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _players),
                Loc.GetString("cmd-pdamute-hint"))
            : CompletionResult.Empty;
    }
}
