using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddObjectiveCommand : IConsoleCommand
    {
        public string Command => "addobjective";
        public string Description => "Adds an objective to the player's mind.";
        public string Help => "addobjective <username> <objectiveID>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (!mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                shell.WriteLine("Can't find the playerdata.");
                return;
            }


            var mind = data.ContentData()?.Mind;
            if (mind == null)
            {
                shell.WriteLine("Can't find the mind.");
                return;
            }

            if (!IoCManager.Resolve<IPrototypeManager>()
                .TryIndex<ObjectivePrototype>(args[1], out var objectivePrototype))
            {
                shell.WriteLine($"Can't find matching ObjectivePrototype {objectivePrototype}");
                return;
            }

            if (!mind.TryAddObjective(objectivePrototype))
            {
                shell.WriteLine("Objective requirements dont allow that objective to be added.");
            }

        }
    }
}
