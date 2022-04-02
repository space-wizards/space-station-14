using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Communications;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationGoals
{
    /// <summary>
    ///     Station goal is generated on round start
    /// </summary>
    public sealed class StationGoalSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public StationGoalPrototype Goal { get; private set; } = default!;

        public override void Initialize()
        {
            base.Initialize();
            PickRandomGoal();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
            SubscribeLocalEvent<RoundStartedEvent>(OnStarted);
        }

        private void OnRestart(RoundRestartCleanupEvent ev)
        {
            PickRandomGoal();
        }

        private void OnStarted(RoundStartedEvent ev)
        {
            SendStationGoal();
        }

        public bool TryFindStationGoal(string goalId, [NotNullWhen(true)] out StationGoalPrototype? prototype)
        {
            prototype = FindStationGoal(goalId);
            return prototype != null;
        }

        public StationGoalPrototype? FindStationGoal(string goalId)
        {
            _prototypeManager.TryIndex(goalId, out StationGoalPrototype? goalProto);
            return goalProto;
        }

        public void SetStationGoal(StationGoalPrototype goal)
        {
            Goal = goal;
        }

        /// <summary>
        ///     Pick random station goal. Replacing old one.
        /// </summary>
        public void PickRandomGoal()
        {
            var goals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>();
            Goal = _random.Pick(goals.ToList());
        }

        /// <summary>
        ///     Send a station goal to all communication consoles
        /// </summary>
        /// <returns>True if at least one console received goal</returns>
        public bool SendStationGoal()
        {
            // todo: this should probably be handled by fax system
            var wasSent = false;
            var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>();
            foreach (var console in consoles)
            {
                if (!EntityManager.TryGetComponent((console).Owner, out TransformComponent? transform))
                    continue;

                var consolePos = transform.MapPosition;
                EntityManager.SpawnEntity("StationGoalPaper", consolePos);

                wasSent = true;
            }

            return wasSent;
        }
    }
}
