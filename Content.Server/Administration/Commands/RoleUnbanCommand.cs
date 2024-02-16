using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleUnbanCommand : IConsoleCommand
{
    public string Command => "roleunban";
    public string Description => Loc.GetString("cmd-roleunban-desc");
    public string Help => Loc.GetString("cmd-roleunban-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var banId))
        {
            shell.WriteLine($"Unable to parse {args[0]} as a ban id integer.\n{Help}");
            return;
        }

        var banManager = IoCManager.Resolve<IBanManager>();
        var response = await banManager.PardonRoleBan(banId, shell.Player?.UserId, DateTimeOffset.Now);
        shell.WriteLine(response);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Can't think of good way to do hint options for this
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-roleunban-hint-1")),
            _ => CompletionResult.Empty
        };
    }
}
