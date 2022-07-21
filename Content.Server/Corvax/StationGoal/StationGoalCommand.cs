using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using JetBrains.Annotations;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     Admin command to set station goal by id.
    /// </summary>
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class StationGoalCommand : IConsoleCommand
    {
        public string Command => "setstationgoal";
        public string Description => "Send station goal paper to communication consoles";
        public string Help => "setstationgoal <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("No station goal specified!");
                return;
            }

            var id = args[0];
            if (!EntitySystem.Get<StationGoalSystem>().CreateStationGoalById(id))
            {
                shell.WriteError($"No station goal with id={id} has been found!");
            }
        }
    }
}
