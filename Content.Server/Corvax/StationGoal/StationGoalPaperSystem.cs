using Content.Server.Communications;
using Content.Server.Paper;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     System to spawn paper with station goal.
    /// </summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly PaperSystem _paperSystem = default!;

        public void SpawnStationGoalPaper(StationGoalPrototype goal)
        {
            var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>();
            foreach (var console in consoles)
            {
                if (!EntityManager.TryGetComponent((console).Owner, out TransformComponent? transform))
                    continue;

                var consolePos = transform.MapPosition;
                var paperId = EntityManager.SpawnEntity("StationGoalPaper", consolePos);
                var paper = Comp<PaperComponent>(paperId);

                _paperSystem.SetContent(paper.Owner, Loc.GetString(goal.Text));
            }
        }
    }
}
