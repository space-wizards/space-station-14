using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.None)]
    public sealed class PrayCommand : IConsoleCommand
    {

        public string Command => "pray";
        public string Description => Loc.GetString("prayer-command-description");
        public string Help => Loc.GetString("prayer-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // TODO: Actually make this work
            var prayerSystem = IoCManager.Resolve<PrayerSystem>();

            if (shell.Player == null)
            {
                shell.WriteLine("You have no player, are you running this on the server?");
                return;
            }
            prayerSystem.Pray(shell.Player, argStr);
        }
    }
}
