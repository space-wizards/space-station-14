#nullable enable
using JetBrains.Annotations;
using System.Linq;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Access;
using Content.Server.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class Greytide : StationEvent
    {
        public override string Name => "Greytide";
        protected override string StartAnnouncement => Loc.GetString(
            "Gr3y.T1d3 virus detected in the station's door subroutines. Severity level of[severity]. Recommend station AI involvement.");
        public override StationEventWeight Weight => StationEventWeight.Low;
        public override int MinimumPlayers => 5;
        public override int? MaxOccurrences => 2;
        protected override float AnnounceWhen => 50.0f; // default vars.
        protected override float EndWhen => 20.0f;

        private int _severity = 1;

        // Access IDs the event will target
        private readonly List<string> _eventTargets = new List<string>();

        public override void Initialize()
        {
            base.Initialize();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            AnnounceWhen = robustRandom.Next(50, 60);
            EndWhen = robustRandom.Next(20, 30);
            _severity = robustRandom.Next(1, 3);

            var accessHelper = new AccessHelper();
            // possible event target(s)

            var posEventTarget = new[] {
                AccessHelper.DoorSector.Security, AccessHelper.DoorSector.Command,
                AccessHelper.DoorSector.Engine, AccessHelper.DoorSector.Medical,
                AccessHelper.DoorSector.Science, AccessHelper.DoorSector.Cargo };

            // severity == department count
            for (int i = 0; i < _severity; i++)
            {
                var diceRolled = robustRandom.Next(0, posEventTarget.Length);
                var selected = posEventTarget[diceRolled];
                // try to get the selected 
                accessHelper.TryGetDepartmentDoorNames(selected, out var access);
                _eventTargets.Concat(access);
                posEventTarget.Take(diceRolled);
            }
        }

        public override void Start()
        {
            // todo: Flicker lights here for spooks
            return;
        }

        public override void Announce()
        {
            // special announcement. todo: station name
            StartAnnouncement = "Gr3y.T1d3 virus detected in the station's door subroutines. Severity level of " + _severity + ". Recommend station AI involvement.";
            base.Announce();
        }

        public override void Shutdown()
        {
            var componentManager = IoCManager.Resolve<IComponentManager>();
            // apc manager, blow the lights

            // secure lockers, open it up

            // unbolt, open, bolt airlocks
            foreach (var airlock in componentManager.EntityQuery<AirlockComponent>())
            {
                var skipMe = false;
                if (airlock.Owner.TryGetComponent<AccessReader>(out var accessReader))
                {
                    // i fucking hate this :salt:
                    foreach (var accessTarget in _eventTargets)
                    {
                        skipMe = accessReader.AccessLists.Any(list => list.All(accessTarget.Contains));
                        if (skipMe) continue;
                    }
                    if (skipMe) continue;
                }
                // Event flow: Unbolt -> try to open -> Bolt open (if possible)
                airlock.BoltsDown = false;
                // this is needed so it does not open welded doors.
                if (airlock.CanOpen()) airlock.Open();
                airlock.BoltsDown = true;
            }
            // finish all prison timers
            // no prison timers on ss14 <b>yet</b>.
        }
    }
}
