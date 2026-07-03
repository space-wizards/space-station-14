using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed partial class SetChatCommand : LocalizedCommands
{
    // Makes look-up easier, and in general the code is more readable.
    private static readonly Dictionary<string, (CVarDef<bool> CVar, string LocPrefix)> ChatMap = new()
    {
        [DeadChat] = (CCVars.DeadChatEnabled, "cmd-setdeadchat-looc"),
        [LoocChat] = (CCVars.LoocEnabled, "cmd-setlooc-looc"),
        [OocChat] = (CCVars.OocEnabled, "cmd-setooc-ooc"),
    };

    private const string DeadChat = "dead";
    private const string LoocChat = "looc";
    private const string OocChat = "ooc";

    [Dependency] private IConfigurationManager _configManager = default!;

    public override string Command => "setchat";

    public override string Description => "Toggles the specified chat on/off.";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 2)));
            return;
        }

        // If we can't find the chat name in the look-up, send an error.
        if (!ChatMap.TryGetValue(args[0], out var entry))
        {
            shell.WriteError("shell-unknown-error"); // Needs to be its own separate error message, I think
            return;
        }

        bool enabled;
        if (args[1] == string.Empty)
        {
            enabled = !_configManager.GetCVar(entry.CVar);
        }
        else
        {
            if (!bool.TryParse(args[1], out enabled))
            {
                shell.WriteError("shell-invalid-bool");
                return;
            }
        }

        _configManager.SetCVar(entry.CVar, enabled);
        shell.WriteLine(Loc.GetString(enabled ? $"{entry.LocPrefix}-enabled" : $"{entry.LocPrefix}-disabled"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions([DeadChat, LoocChat, OocChat]),
            2 => CompletionResult.FromOptions(CompletionHelper.Booleans),
            _ => CompletionResult.Empty,
        };
    }
}
