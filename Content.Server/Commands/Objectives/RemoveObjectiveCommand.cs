#nullable enable
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Objectives
{
    [AdminCommand(AdminFlags.Admin)]
    public class RemoveObjectiveCommand : IClientCommand
    {
        public string Command => "rmobjective";
        public string Description => "Removes an objective from the player's mind.";
        public string Help => "rmobjective <username> <index>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData()?.Mind;
                if (mind == null)
                {
                    shell.SendText(player, "Can't find the mind.");
                    return;
                }

                if (int.TryParse(args[1], out var i))
                {
                    shell.SendText(player,
                        mind.TryRemoveObjective(i)
                            ? "Objective successfully removed!"
                            : "Objective removing failed. Maybe the index is out of bounds? Check lsobjectives!");
                }
                else
                {
                    shell.SendText(player, $"Invalid index {args[1]}!");
                }
            }
            else
            {
                shell.SendText(player, "Can't find the playerdata.");
            }
        }
    }
}
