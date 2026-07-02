using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed partial class SetDeadChatCommand : LocalizedCommands
{
    [Dependency] private IConfigurationManager _configManager = default!;

    public override string Command => "setdeadchat";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        var deadchat = _configManager.GetCVar(CCVars.DeadChatEnabled);

        if (args.Length == 0)
        {
            deadchat = !deadchat;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out deadchat))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        _configManager.SetCVar(CCVars.DeadChatEnabled, deadchat);

        shell.WriteLine(Loc.GetString(deadchat ? "cmd-setdeadchat-looc-enabled" : "cmd-setdeadchat-looc-disabled"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? CompletionResult.FromOptions(CompletionHelper.Booleans) : CompletionResult.Empty;
    }
}
