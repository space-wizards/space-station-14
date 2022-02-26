using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Salvage
{
    public sealed class SalvageSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        private static readonly TimeSpan AttachingTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan HoldTime = TimeSpan.FromMinutes(4);
        private static readonly TimeSpan DetachingTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CooldownTime = TimeSpan.FromMinutes(1);

        private readonly Dictionary<GridId, SalvageGridState> _salvageGridStates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SalvageMagnetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SalvageMagnetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SalvageMagnetComponent, ComponentShutdown>(OnMagnetRemoval);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

            // Can't use RoundRestartCleanupEvent, I need to clean up before the grid, and components are gone to prevent the announcements
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        }

        private void OnRoundEnd(GameRunLevelChangedEvent ev)
        {
            if(ev.New != GameRunLevel.InRound)
            {
                _salvageGridStates.Clear();
            }
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            // If we ever want to give magnets names, and announce them individually, we would need to loop this, before removing it.
            if (_salvageGridStates.Remove(ev.GridId))
            {
                Report("salvage-system-announcement-spawn-magnet-lost");
                // For the very unlikely possibility that the salvage magnet was on a salvage, we will not return here
            }
            foreach(var gridState in _salvageGridStates)
            {
                foreach(var magnet in gridState.Value.ActiveMagnets)
                {
                    if (magnet.AttachedEntity == ev.EntityUid)
                    {
                        magnet.AttachedEntity = null;
                        magnet.MagnetState = MagnetState.Inactive;
                        Report("salvage-system-announcement-lost");
                        return;
                    }
                }
            }
        }

        private void OnMagnetRemoval(EntityUid uid, SalvageMagnetComponent component, ComponentShutdown args)
        {
            if (component.MagnetState.StateType == MagnetStateType.Inactive) return;

            var magnetTranform = EntityManager.GetComponent<TransformComponent>(component.Owner);
            if (!_salvageGridStates.TryGetValue(magnetTranform.GridID, out var salvageGridState))
            {
                return;
            }
            salvageGridState.ActiveMagnets.Remove(component);
            Report("salvage-system-announcement-spawn-magnet-lost");
            if (component.AttachedEntity.HasValue)
            {
                SafeDeleteSalvage(component.AttachedEntity.Value);
                component.AttachedEntity = null;
                Report("salvage-system-announcement-lost");
            }
            else if (component.MagnetState is { StateType: MagnetStateType.Attaching })
            {
                Report("salvage-system-announcement-spawn-no-debris-available");
            }
            component.MagnetState = MagnetState.Inactive;
        }

        private void OnExamined(EntityUid uid, SalvageMagnetComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            switch (component.MagnetState.StateType)
            {
                case MagnetStateType.Inactive:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-inactive"));
                    break;
                case MagnetStateType.Attaching:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-pulling-in"));
                    break;
                case MagnetStateType.Detaching:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-releasing"));
                    break;
                case MagnetStateType.CoolingDown:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-cooling-down"));
                    break;
                case MagnetStateType.Holding:
                    var magnetTranform = EntityManager.GetComponent<TransformComponent>(component.Owner);
                    if (_salvageGridStates.TryGetValue(magnetTranform.GridID, out var salvageGridState))
                    {
                        var remainingTime = component.MagnetState.Until - salvageGridState.CurrentTime;
                        args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-active", ("timeLeft", remainingTime.TotalSeconds)));
                    }
                    else
                    {
                        Logger.WarningS("salvage", "Failed to load salvage grid state, can't display remaining time");
                    }
                    break;
                default:
                    throw new NotImplementedException("Unexpected magnet state type");
            }
        }

        private void OnInteractHand(EntityUid uid, SalvageMagnetComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = true;
            StartMagnet(component, args.User);
        }

        private void StartMagnet(SalvageMagnetComponent component, EntityUid user)
        {
            switch (component.MagnetState.StateType)
            {
                case MagnetStateType.Inactive:
                    ShowPopup("salvage-system-report-activate-success", component, user);
                    var magnetTranform = EntityManager.GetComponent<TransformComponent>(component.Owner);
                    SalvageGridState? gridState;
                    if (!_salvageGridStates.TryGetValue(magnetTranform.GridID, out gridState))
                    {
                        gridState = new SalvageGridState();
                        _salvageGridStates[magnetTranform.GridID] = gridState;
                    }
                    gridState.ActiveMagnets.Add(component);
                    component.MagnetState = new MagnetState(MagnetStateType.Attaching, gridState.CurrentTime + AttachingTime);
                    Report("salvage-system-report-activate-success");
                    break;
                case MagnetStateType.Attaching:
                case MagnetStateType.Holding:
                    ShowPopup("salvage-system-report-already-active", component, user);
                    break;
                case MagnetStateType.Detaching:
                case MagnetStateType.CoolingDown:
                    ShowPopup("salvage-system-report-cooling-down", component, user);
                    break;
                default:
                    throw new NotImplementedException("Unexpected magnet state type");
            }
        }
        private void ShowPopup(string messageKey, SalvageMagnetComponent component, EntityUid user)
        {
            _popupSystem.PopupEntity(Loc.GetString(messageKey), component.Owner, Filter.Entities(user));
        }

        private void SafeDeleteSalvage(EntityUid salvage)
        {
            if(!EntityManager.TryGetComponent<TransformComponent>(salvage, out var salvageTransform))
            {
                Logger.ErrorS("salvage", "Salvage entity was missing transform component");
                return;
            }

            var parentTransform = salvageTransform.Parent!;
            foreach (var player in Filter.Empty().AddInGrid(salvageTransform.GridID, EntityManager).Recipients)
            {
                if (player.AttachedEntity.HasValue)
                {
                    var playerTransform = EntityManager.GetComponent<TransformComponent>(player.AttachedEntity.Value);
                    playerTransform.AttachParent(parentTransform);
                }
            }
            EntityManager.QueueDeleteEntity(salvage);
        }

        private bool TryGetSalvagePlacementLocation(out MapCoordinates coords, out Angle angle)
        {
            coords = MapCoordinates.Nullspace;
            angle = Angle.Zero;
            foreach (var (smc, tsc) in EntityManager.EntityQuery<SalvageMagnetComponent, TransformComponent>(true))
            {
                coords = new EntityCoordinates(smc.Owner, smc.Offset).ToMap(EntityManager);
                var grid = tsc.GridID;
                if (_mapManager.TryGetGrid(grid, out var magnetGrid))
                {
                    angle = magnetGrid.WorldRotation;
                }
                return true;
            }
            return false;
        }

        private IEnumerable<SalvageMapPrototype> GetAllSalvageMaps() =>
            _prototypeManager.EnumeratePrototypes<SalvageMapPrototype>();

        private bool SpawnSalvage(SalvageMagnetComponent component)
        {
            if (!TryGetSalvagePlacementLocation(out var spl, out var spAngle))
            {
                Report("salvage-system-announcement-spawn-magnet-lost");
                return false;
            }

            SalvageMapPrototype? map = null;

            var forcedSalvage = _configurationManager.GetCVar<string>(CCVars.SalvageForced);
            List<SalvageMapPrototype> allSalvageMaps;
            if (string.IsNullOrWhiteSpace(forcedSalvage))
            {
                allSalvageMaps = GetAllSalvageMaps().ToList();
            }
            else
            {
                allSalvageMaps = new();
                if (_prototypeManager.TryIndex<SalvageMapPrototype>(forcedSalvage, out map))
                {
                    allSalvageMaps.Add(map);
                }
                else
                {
                    Logger.ErrorS("c.s.salvage", $"Unable to get forced salvage map prototype {forcedSalvage}");
                }
            }
            for (var i = 0; i < allSalvageMaps.Count; i++)
            {
                map = _random.PickAndTake(allSalvageMaps);
                var box2 = Box2.CenteredAround(spl.Position, new Vector2(map.Size * 2.0f, map.Size * 2.0f));
                var box2rot = new Box2Rotated(box2, spAngle, spl.Position);

                // This doesn't stop it from spawning on top of random things in space
                // Might be better like this, ghosts could stop it before
                if (_mapManager.FindGridsIntersecting(spl.MapId, box2rot).Any())
                {
                    map = null;
                }
                else
                {
                    break;
                }
            }

            if (map == null)
            {
                Report("salvage-system-announcement-spawn-no-debris-available");
                return false;
            }
            var bp = _mapLoader.LoadBlueprint(spl.MapId, map.MapPath.ToString());
            if (bp == null)
            {
                Report("salvage-system-announcement-spawn-debris-disintegrated");
                return false;
            }
            var salvageEntityId = bp.GridEntityId;
            component.AttachedEntity = salvageEntityId;

            var pulledTransform = EntityManager.GetComponent<TransformComponent>(salvageEntityId);
            pulledTransform.Coordinates = EntityCoordinates.FromMap(_mapManager, spl);
            pulledTransform.WorldRotation = spAngle;

            Report("salvage-system-announcement-arrived", ("timeLeft", HoldTime.TotalSeconds));
            return true;
        }
        private void Report(string messageKey) =>
            _chatManager.DispatchStationAnnouncement(Loc.GetString(messageKey), Loc.GetString("salvage-system-announcement-source"));
        private void Report(string messageKey, params (string, object)[] args) =>
            _chatManager.DispatchStationAnnouncement(Loc.GetString(messageKey, args), Loc.GetString("salvage-system-announcement-source"));

        private void Transition(SalvageMagnetComponent magnet, TimeSpan currentTime)
        {
            switch (magnet.MagnetState.StateType)
            {
                case MagnetStateType.Attaching:
                    if (SpawnSalvage(magnet))
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.Holding, currentTime + HoldTime);
                    }
                    else
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + CooldownTime);
                    }
                    break;
                case MagnetStateType.Holding:
                    Report("salvage-system-announcement-losing", ("timeLeft", DetachingTime.TotalSeconds));
                    magnet.MagnetState = new MagnetState(MagnetStateType.Detaching, currentTime + DetachingTime);
                    break;
                case MagnetStateType.Detaching:
                    if (magnet.AttachedEntity.HasValue)
                    {
                        SafeDeleteSalvage(magnet.AttachedEntity.Value);
                    }
                    else
                    {
                        Logger.ErrorS("salvage", "Salvage detaching was expecting attached entity but it was null");
                    }
                    Report("salvage-system-announcement-lost");
                    magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + CooldownTime);
                    break;
                case MagnetStateType.CoolingDown:
                    magnet.MagnetState = MagnetState.Inactive;
                    break;
            }
        }

        public override void Update(float frameTime)
        {
            var secondsPassed = TimeSpan.FromSeconds(frameTime);
            // Keep track of time, and state per grid
            foreach (var gridIdAndState in _salvageGridStates)
            {
                var state = gridIdAndState.Value;
                if (state.ActiveMagnets.Count == 0) continue;
                var gridId = gridIdAndState.Key;
                // Not handling the case where the salvage we spawned got paused
                // They both need to be paused, or it doesn't make sense
                if (_mapManager.IsGridPaused(gridId)) continue;
                state.CurrentTime += secondsPassed;

                var deleteQueue = new RemQueue<SalvageMagnetComponent>();
                foreach(var magnet in state.ActiveMagnets)
                {
                    if (magnet.MagnetState.Until > state.CurrentTime) continue;
                    Transition(magnet, state.CurrentTime);
                    if (magnet.MagnetState.StateType == MagnetStateType.Inactive)
                    {
                        deleteQueue.Add(magnet);
                    }
                }

                foreach(var magnet in deleteQueue)
                {
                    state.ActiveMagnets.Remove(magnet);
                }
            }
        }
    }

    public sealed class SalvageGridState
    {
        public TimeSpan CurrentTime { get; set; }
        public List<SalvageMagnetComponent> ActiveMagnets { get; } = new();
    }
}

