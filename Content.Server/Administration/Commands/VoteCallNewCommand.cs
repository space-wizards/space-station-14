using Content.Server.VotingNew;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class VoteCallNewCommand : IConsoleCommand
{
    public string Command => "votecallui";

    public string Description => "Opens the new vote call window";

    public string Help => $"{Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("This does not work from the server console.");
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new VoteCallNewEui();
        eui.OpenEui(ui, player);
    }
}
