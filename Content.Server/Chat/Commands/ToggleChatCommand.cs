using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed partial class ToggleChatCommand : LocalizedCommands
{
    [Dependency] private IConfigurationManager _configManager = default!;

    private const string DeadChat = "Dead";
    private const string LoocChat = "LOOC";
    private const string OocChat = "OOC";

    // Makes look-up easier, and in general the code is more readable.
    private static readonly Dictionary<string, (CVarDef<bool> CVar, string LocPrefix)> ChatMap = new()
    {
        [DeadChat] = (CCVars.DeadChatEnabled, "cmd-setdeadchat-looc"),
        [LoocChat] = (CCVars.LoocEnabled, "cmd-setlooc-looc"),
        [OocChat] = (CCVars.OocEnabled, "cmd-setooc-ooc"),
    };

    public override string Command => "togglechat";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is > 2 or 0)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 2)));
            return;
        }

        // If we can't find the chat name in the look-up, send an error.
        if (!ChatMap.TryGetValue(args[0], out var chat))
        {
            shell.WriteError(Loc.GetString("shell-argument-chat-invalid", ("index", 0)));
            return;
        }

        bool enabled;
        if (args.Length == 1)
        {
            enabled = !_configManager.GetCVar(chat.CVar);
        }
        else if (!bool.TryParse(args[1], out enabled))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        _configManager.SetCVar(chat.CVar, enabled);
        shell.WriteLine(Loc.GetString(enabled ? $"{chat.LocPrefix}-enabled" : $"{chat.LocPrefix}-disabled"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(ChatMap.Keys),
            2 => CompletionResult.FromOptions(CompletionHelper.Booleans),
            _ => CompletionResult.Empty,
        };
    }
}
