using Content.Server.Doors.Systems;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.SCP
{
    internal sealed class ScpBreakoutEvent : StationEventSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoorSystem _doorSys = default!;
        [Dependency] private readonly ApcSystem _apcSystem = default!;
        [Dependency] private readonly SCPGeneralSystem _scpSys = default!;
        //[Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override string Prototype => "ScpBreakout";

        //private CancellationTokenSource? _announceCancelToken;
        private CancellationTokenSource? _breachCancelToken;

        private readonly List<EntityUid> _eventedDoors = new();
        private readonly List<EntityUid> _eventedApcs = new();

        private EntityUid _targetStation = EntityUid.Invalid;
        private float _endAfter = 60;
        public override void Added()
        {
            var targetmap = GameTicker.DefaultMap;
            foreach (var grid in _mapManager.GetAllMapGrids(targetmap))
            {
                if (!TryComp<StationMemberComponent>(grid.Owner, out var stationMember)) continue;
                if (!HasComp<StationDataComponent>(stationMember.Station)) continue;
                _targetStation = stationMember.Station;
                break;
            }
            if (_targetStation == EntityUid.Invalid) { ForceEndSelf(); return; };

            base.Added();

            _endAfter = RobustRandom.Next(40, 80);
        }

        public override void Started()
        {
            if (Configuration is not StationEventRuleConfiguration data)
                return;
            _breachCancelToken?.Cancel();
            _breachCancelToken = new CancellationTokenSource();
            // Why the fuck StationEvents cant handle startAfter timer?
            Timer.Spawn((int)data.StartAfter * 1000, StartBreach, _breachCancelToken.Token);
        }

        private void StartBreach()
        {
            foreach (var (apc, transform) in EntityQuery<ApcComponent, TransformComponent>(true))
            {
                if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == _targetStation)
                {
                    var apcEnt = apc.Owner;
                    _apcSystem.ApcToggleBreaker(apcEnt, apc);
                    _eventedApcs.Add(apcEnt);
                }
            }

            base.Started();

            var groups = _scpSys.GetGroupedDoorsGroups();
			if (groups.Count == 0)
				return;
            var selected = new List<string>();
            for (int i = RobustRandom.Next(1, groups.Count); i != 0; i--)
            {
                var sel = RobustRandom.Pick(groups);
                if (selected.Contains(sel))
                    continue;
                selected.Add(sel);
                foreach (var uid in _scpSys.GetGroupedDoors(sel))
                {
                    if (!TryComp<ScpContainmentDoorComponent>(uid, out var scpDoor))
                        continue;
                    _doorSys.TryOpen(uid);
                    scpDoor.DoorBlocked = true;
                    _eventedDoors.Add(uid);
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
                return;
            }
        }
        public override void Ended()
        {
            base.Ended();

            foreach (var apcEnt in _eventedApcs)
            {
                if (EntityManager.Deleted(apcEnt)) continue;

                if (EntityManager.TryGetComponent(apcEnt, out ApcComponent? apcComponent))
                {
                    if (!apcComponent.MainBreakerEnabled)
                        _apcSystem.ApcToggleBreaker(apcEnt, apcComponent);
                }
            }

            foreach (var uid in _eventedDoors)
            {
                if (!TryComp<ScpContainmentDoorComponent>(uid, out var scpDoor))
                    continue;
                scpDoor.DoorBlocked = false;
            }

            // Can't use the default EndAudio
            //_announceCancelToken?.Cancel();
            //_announceCancelToken = new CancellationTokenSource();
            //Timer.Spawn(3000, () =>
            //{
            //    _audioSystem.PlayGlobal("/Audio/Announcements/power_on.ogg", Filter.Broadcast(), true, AudioParams.Default.WithVolume(-4f));
            //}, _announceCancelToken.Token);

            _eventedApcs.Clear();
            _eventedDoors.Clear();
        }
    }
}
