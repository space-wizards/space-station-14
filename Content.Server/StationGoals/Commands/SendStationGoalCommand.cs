using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.StationGoals.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SendStationGoalCommand : IConsoleCommand
    {
        public string Command => "stationgoal";
        public string Description => "Send station goal to the communication console";
        public string Help => "stationgoal [goalId]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var stationGoal = EntitySystem.Get<StationGoalSystem>();

            if (args.Length > 0)
            {
                var goalId = args[^1];
                if (!stationGoal.TryFindStationGoal(goalId, out var goalProto))
                {
                    shell.WriteLine($"No goal exists with id {goalId}.");
                    return;
                }

                stationGoal.SetStationGoal(goalProto);
            }
            else
            {
                stationGoal.PickRandomGoal();
            }

            EntitySystem.Get<StationGoalSystem>().SendStationGoal();
        }
    }
}
