using System.Linq;
using Content.Server.Fax;
using Content.Server.Paper;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Photocopier;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     System to spawn paper with station goal.
    /// </summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            SendRandomGoal();
        }

        public bool SendRandomGoal()
        {
            var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>().ToList();
            var goal = _random.Pick(availableGoals);
            return SendStationGoal(goal);
        }

        /// <summary>
        ///     Send a station goal to all faxes which are authorized to receive it.
        /// </summary>
        /// <returns>True if at least one fax received paper</returns>
        public bool SendStationGoal(StationGoalPrototype goal)
        {
            var faxes = EntityManager.EntityQuery<FaxMachineComponent>();
            var wasSent = false;
            foreach (var fax in faxes)
            {
                if (!fax.ReceiveStationGoal) continue;

                var dataToCopy = new Dictionary<Type, IPhotocopiedComponentData>();
                var paperDataToCopy = new PaperPhotocopiedData()
                {
                    Content = Loc.GetString(goal.Text),
                    StampState = "paper_stamp-centcom",
                    StampedBy = new List<StampDisplayInfo>{
                        new(){
                            StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                            StampedColor = Color.FromHex("#006600")
                        }
                    }
                };
                dataToCopy.Add(typeof(PaperComponent), paperDataToCopy);

                var metaData = new PhotocopyableMetaData()
                {
                    EntityName = Loc.GetString("station-goal-fax-paper-name"),
                    PrototypeId = "PaperNtFormCc"
                };

                var printout = new FaxPrintout(dataToCopy, metaData);
                _faxSystem.Receive(fax.Owner, printout, null, fax);

                wasSent = true;
            }

            return wasSent;
        }
    }
}
