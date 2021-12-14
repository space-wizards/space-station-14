using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Examine;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Salvage
{
    public class SalvageSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        private static readonly TimeSpan AttachingTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan HoldTime = TimeSpan.FromMinutes(4);
        private static readonly TimeSpan DetachingTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CooldownTime = TimeSpan.FromMinutes(1);
        public const float AngularVelocityRangeRadians = 0.25f;

        private readonly Dictionary<GridId, SalvageGridState> _salvageGridStates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SalvageMagnetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SalvageMagnetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SalvageMagnetComponent, ComponentShutdown>(OnMagnetRemoval);
        }

        private void OnMagnetRemoval(EntityUid uid, SalvageMagnetComponent component, ComponentShutdown args)
        {
            if (component.MagnetState.StateType == MagnetStateType.Inactive) return;
            Report("salvage-system-announcement-spawn-magnet-lost");

            var magnetTranform = EntityManager.GetComponent<TransformComponent>(component.Owner);
            if (_salvageGridStates.TryGetValue(magnetTranform.GridID, out var salvageGridState))
            {
                salvageGridState.ActiveMagnets.Remove(component);
            }
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
                    SalvageGridState? gridState = null;
                    if (_salvageGridStates.TryGetValue(magnetTranform.GridID, out gridState))
                    {
                        gridState.ActiveMagnets.Add(component);
                    }
                    else
                    {
                        gridState = new SalvageGridState();
                        gridState.ActiveMagnets.Add(component);
                        _salvageGridStates[magnetTranform.GridID] = gridState;
                    }
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
            //TODO: Figure out how to delete a grid with players, and ghosts on it without crashing anything.
            foreach(var player in Filter.Empty().AddInGrid(salvageTransform.GridID, EntityManager).Recipients)
            {
                if (player.AttachedEntity.HasValue)
                {
                    var playerTranform = EntityManager.GetComponent<TransformComponent>(player.AttachedEntity.Value);
                    playerTranform.AttachParent(salvageTransform.Parent!);
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
                if (grid != GridId.Invalid)
                {
                    // Has a valid grid - synchronize angle so that salvage doesn't have to deal with cross-grid manipulation issues
                    angle = _mapManager.GetGrid(grid).WorldRotation;
                }
                return true;
            }
            return false;
        }

        private IEnumerable<SalvageMapPrototype> GetAllSalvageMaps() =>
            _prototypeManager.EnumeratePrototypes<SalvageMapPrototype>();

        private void SpawnSalvage(SalvageMagnetComponent component)
        {
            if (!TryGetSalvagePlacementLocation(out var spl, out var spAngle))
            {
                Report("salvage-system-announcement-spawn-magnet-lost");
                return;
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
                if (_physicsSystem.GetCollidingEntities(spl.MapId, in box2rot).Select(x => EntityManager.HasComponent<IMapGridComponent>(x.Owner)).Count() > 0)
                {
                    // collided: set map to null so we don't spawn it
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
                return;
            }

            var bp = _mapLoader.LoadBlueprint(spl.MapId, map.MapPath);
            if (bp == null)
            {
                Report("salvage-system-announcement-spawn-debris-disintegrated");
                return;
            }
            var salvageEntityId = bp.GridEntityId;
            component.AttachedEntity = salvageEntityId;

            var pulledTransform = EntityManager.GetComponent<TransformComponent>(salvageEntityId);
            pulledTransform.Coordinates = EntityCoordinates.FromMap(_mapManager, spl);
            pulledTransform.WorldRotation = spAngle;

            Report("salvage-system-announcement-arrived", ("timeLeft", HoldTime.TotalSeconds));
        }
        private void Report(string messageKey) =>
            _chatManager.DispatchStationAnnouncement(Loc.GetString(messageKey), Loc.GetString("salvage-system-announcement-source"));
        private void Report(string messageKey, params (string, object)[] args) =>
            _chatManager.DispatchStationAnnouncement(Loc.GetString(messageKey, args), Loc.GetString("salvage-system-announcement-source"));

        private void Transition(SalvageMagnetComponent magnet, TimeSpan currentTime)
        {
            //Add announces/actions
            switch (magnet.MagnetState.StateType)
            {
                case MagnetStateType.Attaching:
                    SpawnSalvage(magnet);
                    magnet.MagnetState = new MagnetState(MagnetStateType.Holding, currentTime + HoldTime);
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
            foreach (var gridIdAndState in _salvageGridStates)
            {
                var state = gridIdAndState.Value;
                if (state.ActiveMagnets.Count == 0) continue;
                var gridId = gridIdAndState.Key;
                // Not handling the case where the salvage we spawned got paused
                // They both need to be paused, or it doesn't make sense
                if (_pauseManager.IsGridPaused(gridId)) continue;
                state.CurrentTime += TimeSpan.FromSeconds(frameTime);
                var currentMagnet = 0;
                while (currentMagnet < state.ActiveMagnets.Count)
                {
                    var magnet = state.ActiveMagnets[currentMagnet];
                    if (magnet.MagnetState.Until > state.CurrentTime)
                    {
                        currentMagnet++;
                        continue;
                    }
                    Transition(magnet, state.CurrentTime);
                    if (magnet.MagnetState.StateType == MagnetStateType.Inactive)
                    {
                        state.ActiveMagnets.RemoveAt(currentMagnet);
                    }
                    else
                    {
                        currentMagnet++;
                    }
                }
            }
        }
    }

    public class SalvageGridState
    {
        public TimeSpan CurrentTime { get; set; }
        public List<SalvageMagnetComponent> ActiveMagnets { get; } = new();
    }
}

