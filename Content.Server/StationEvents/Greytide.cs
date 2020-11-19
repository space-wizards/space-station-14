#nullable enable
using JetBrains.Annotations;
using System.Linq;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Access;
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
        // [station_name()] => "the station's"
        public override StationEventWeight Weight => StationEventWeight.Low;
        public override int MinimumPlayers => 5;
        public override int? MaxOccurrences => 2;
        protected override float AnnounceWhen => 50.0f; // default vars.
        protected override float EndWhen => 20.0f;

        private int _severity = 1;

        // Access-like for airlocks, because we don't have areas
        private string[] _secLike = { "Security", "Brig" };
        private string[] _comandLike = { "Command", "Bridge"};
        private string[] _engineLike = { "Engineering" };
        private string[] _medicalLike = { "Medical" };
        private string[] _cargoLike = { "Cargo" };
        private string[] _scienceLike = { "Science", "Research And Development"};

        private string[][] _eventTargets = { }; // Access IDs the event will target

        public override void Setup()
        {
            base.Setup();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();

            AnnounceWhen = robustRandom.Next(50, 60);
            EndWhen = robustRandom.Next(20, 30);
            _severity = robustRandom.Next(1, 3);

            // possible event target(s)
            var posEventTarget = new[] { _secLike, _comandLike, _engineLike, _medicalLike, _cargoLike, _scienceLike };
            for (int i = 0; i < _severity; i++)
            {
                var pickedArea = posEventTarget.Take(robustRandom.Next(0, posEventTarget.Length)).ToList();
                _eventTargets.Union(pickedArea); // why isn't union unionizing the strings inside the list :salt:
            }
        }

        public override void Start()
        {
            // bannanabread. Flicker lights here for spooks
            return;
        }
        public override void End()
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
        }
    }
}
