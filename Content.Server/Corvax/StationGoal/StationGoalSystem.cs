using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.GameTicking;
using System.Linq;
using Content.Shared.Random.Helpers;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     Station goal is set at round start.
    ///     Admin can change the station goal via <see cref="StationGoalCommand"></see>
    /// </summary>
    public sealed class StationGoalSystem : EntitySystem
    {
        [Dependency] private readonly StationGoalPaperSystem _stationGoalPaperSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            CreateRandomStationGoal();
        }

        public void CreateRandomStationGoal()
        {
            var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>();

            var random = IoCManager.Resolve<IRobustRandom>();
            var goal = random.Pick(availableGoals.ToList());
            _stationGoalPaperSystem.SpawnStationGoalPaper(goal);
        }

        public bool CreateStationGoalById(string stationGoalId)
        {
            if (!_prototypeManager.TryIndex(stationGoalId, out StationGoalPrototype? prototype))
                return false;

            _stationGoalPaperSystem.SpawnStationGoalPaper(_prototypeManager.Index<StationGoalPrototype>(stationGoalId));
            return true;
        }
    }
}
