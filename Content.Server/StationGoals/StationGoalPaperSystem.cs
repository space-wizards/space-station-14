using Content.Server.Paper;

namespace Content.Server.StationGoals
{
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly StationGoalSystem _stationGoal = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StationGoalPaperComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, StationGoalPaperComponent component, MapInitEvent args)
        {
            PaperComponent? paper = null;
            if (!Resolve(uid, ref paper))
                return;

            var goalText = Loc.GetString(_stationGoal.Goal.Text);
            paper.Content += Loc.GetString("station-goal-paper-comp-template", ("content", goalText));
        }
    }
}
