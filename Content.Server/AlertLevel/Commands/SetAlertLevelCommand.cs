using Content.Server.Administration;
using Content.Server.AlertLevel;
using Content.Server.Players;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.AlertLevel.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetAlertLevelCommand : IConsoleCommand
    {
        public string Command => "setalertlevel";
        public string Description => "Set current station alert level.";
        public string Help => "setalertlevel <level> [locked]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("Incorrect number of arguments. " + Help);
                return;
            }

            var locked = false;
            if (args.Length > 1 && !bool.TryParse(args[1], out locked))
            {
                shell.WriteLine("Invalid boolean flag");
                return;
            }

            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot run this from the server or without an attached entity.");
                return;
            }

            var playerEntityUid = player.AttachedEntity;
            if (playerEntityUid == null)
            {
                shell.WriteLine("You cannot run this from the server or without an attached entity.");
                return;
            }

            var stationUid = EntitySystem.Get<StationSystem>().GetOwningStation(playerEntityUid.Value);
            if (stationUid == null)
            {
                shell.WriteLine("You must be on grid of station code that you are going to change.");
                return;
            }

            var level = args[0];
            EntitySystem.Get<AlertLevelSystem>().SetLevel(stationUid.Value, level, true, true, true, locked);
        }
    }
}
