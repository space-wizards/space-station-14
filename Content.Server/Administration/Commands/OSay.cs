using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class OSay : LocalizedCommands
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "osay";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("osay-command-arg-uid"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions( Enum.GetNames(typeof(InGameICChatType)),
                Loc.GetString("osay-command-arg-type"));
        }

        if (args.Length > 2)
        {
            return CompletionResult.FromHint(Loc.GetString("osay-command-arg-message"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("osay-command-error-args"));
            return;
        }

        var chatType = (InGameICChatType) Enum.Parse(typeof(InGameICChatType), args[1]);

        if (!NetEntity.TryParse(args[0], out var sourceNet) || !_entityManager.TryGetEntity(sourceNet, out var source) || !_entityManager.EntityExists(source))
        {
            shell.WriteLine(Loc.GetString("osay-command-error-euid", ("arg", args[0])));
            return;
        }

        var message = string.Join(" ", args.Skip(2)).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _entityManager.System<ChatSystem>().TrySendInGameICMessage(source.Value, message, chatType, false);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{(shell.Player != null ? shell.Player.Name : "An administrator")} forced {_entityManager.ToPrettyString(source.Value)} to {args[1]}: {message}");
    }
}
