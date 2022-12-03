using Content.Server.GameTicking;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Salvage
{
    public sealed class SalvageSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        private TimeSpan _attachingTime;
        private TimeSpan _holdTime;
        private TimeSpan _detachingTime;
        private TimeSpan _cooldownTime;
        private static readonly int SalvageLocationPlaceAttempts = 16;

        // TODO: This is probably not compatible with multi-station
        private readonly Dictionary<EntityUid, SalvageGridState> _salvageGridStates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SalvageMagnetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SalvageMagnetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SalvageMagnetComponent, ComponentShutdown>(OnMagnetRemoval);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

            // Can't use RoundRestartCleanupEvent, I need to clean up before the grid, and components are gone to prevent the announcements
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);

            _configurationManager.OnValueChanged(CCVars.SalvageAttachingTime, SetAttachingTime, true);
            _configurationManager.OnValueChanged(CCVars.SalvageHoldTime, SetHoldTime, true);
            _configurationManager.OnValueChanged(CCVars.SalvageDetachingTime, SetDetachingTime, true);
            _configurationManager.OnValueChanged(CCVars.SalvageCooldownTime, SetCooldownTime, true);
        }

        private void SetAttachingTime(int obj)
        {
            _attachingTime = TimeSpan.FromSeconds(obj);
        }

        private void SetHoldTime(int obj)
        {
            _holdTime = TimeSpan.FromSeconds(obj);
        }

        private void SetDetachingTime(int obj)
        {
            _detachingTime = TimeSpan.FromSeconds(obj);
        }

        private void SetCooldownTime(int obj)
        {
            _cooldownTime = TimeSpan.FromSeconds(obj);
        }

        private void OnRoundEnd(GameRunLevelChangedEvent ev)
        {
            if(ev.New != GameRunLevel.InRound)
            {
                _salvageGridStates.Clear();
            }
        }

        private void UpdateAppearance(EntityUid uid, SalvageMagnetComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            _appearanceSystem.SetData(uid, SalvageMagnetVisuals.ReadyBlinking, component.MagnetState.StateType == MagnetStateType.Attaching);
            _appearanceSystem.SetData(uid, SalvageMagnetVisuals.Ready, component.MagnetState.StateType == MagnetStateType.Holding);
            _appearanceSystem.SetData(uid, SalvageMagnetVisuals.Unready, component.MagnetState.StateType == MagnetStateType.CoolingDown);
            _appearanceSystem.SetData(uid, SalvageMagnetVisuals.UnreadyBlinking, component.MagnetState.StateType == MagnetStateType.Detaching);
        }

        private void UpdateChargeStateAppearance(EntityUid uid, TimeSpan currentTime, SalvageMagnetComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            int timeLeft = (component.MagnetState.Until.Minutes * 60 + component.MagnetState.Until.Seconds) - (currentTime.Minutes * 60 + currentTime.Seconds);
            if (component.MagnetState.StateType == MagnetStateType.Inactive)
                component.ChargeRemaining = 5;
            else if (component.MagnetState.StateType == MagnetStateType.Holding)
            {
                if (_holdTime.TotalSeconds > 0)
                {
                    component.ChargeRemaining = (timeLeft / ((_holdTime.Minutes * 60 + _holdTime.Seconds) / component.ChargeCapacity)) + 1;
                }
            }
            else if (component.MagnetState.StateType == MagnetStateType.Detaching)
                component.ChargeRemaining = 0;
            else if (component.MagnetState.StateType == MagnetStateType.CoolingDown)
            {
                if (_cooldownTime.TotalSeconds > 0)
                {
                    component.ChargeRemaining = component.ChargeCapacity - (timeLeft / ((_cooldownTime.Minutes * 60 + _cooldownTime.Seconds) / component.ChargeCapacity)) - 1;
                }
                else
                {
                    component.ChargeRemaining = component.ChargeCapacity;
                }
            }
            if (component.PreviousCharge != component.ChargeRemaining)
            {
                _appearanceSystem.SetData(uid, SalvageMagnetVisuals.ChargeState, component.ChargeRemaining);
                component.PreviousCharge = component.ChargeRemaining;
            }
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            // If we ever want to give magnets names, and announce them individually, we would need to loop this, before removing it.
            if (_salvageGridStates.Remove(ev.EntityUid))
            {
                if (EntityManager.TryGetComponent<SalvageGridComponent>(ev.EntityUid, out var salvComp) && salvComp.SpawnerMagnet != null)
                    Report(salvComp.SpawnerMagnet.Owner, salvComp.SpawnerMagnet.SalvageChannel, "salvage-system-announcement-spawn-magnet-lost");
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
                        return;
                    }
                }
            }
        }

        private void OnMagnetRemoval(EntityUid uid, SalvageMagnetComponent component, ComponentShutdown args)
        {
            if (component.MagnetState.StateType == MagnetStateType.Inactive) return;

            var magnetTranform = EntityManager.GetComponent<TransformComponent>(component.Owner);
            if (!(magnetTranform.GridUid is EntityUid gridId) || !_salvageGridStates.TryGetValue(gridId, out var salvageGridState))
            {
                return;
            }
            salvageGridState.ActiveMagnets.Remove(component);
            Report(uid, component.SalvageChannel, "salvage-system-announcement-spawn-magnet-lost");
            if (component.AttachedEntity.HasValue)
            {
                SafeDeleteSalvage(component.AttachedEntity.Value);
                component.AttachedEntity = null;
                Report(uid, component.SalvageChannel, "salvage-system-announcement-lost");
            }
            else if (component.MagnetState is { StateType: MagnetStateType.Attaching })
            {
                Report(uid, component.SalvageChannel, "salvage-system-announcement-spawn-no-debris-available");
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
                    var magnetTransform = EntityManager.GetComponent<TransformComponent>(component.Owner);
                    if (magnetTransform.GridUid is EntityUid gridId && _salvageGridStates.TryGetValue(gridId, out var salvageGridState))
                    {
                        if (_holdTime.TotalSeconds > 0)
                        {
                            var remainingTime = component.MagnetState.Until - salvageGridState.CurrentTime;
                            args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-active", ("timeLeft", Math.Ceiling(remainingTime.TotalSeconds))));
                        }
                        else
                        {
                            args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-active-timeless"));
                        }
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
            UpdateAppearance(uid, component);
        }

        private void StartMagnet(SalvageMagnetComponent component, EntityUid user)
        {
            var magnetTransform = EntityManager.GetComponent<TransformComponent>(component.Owner);
            SalvageGridState? gridState;
            switch (component.MagnetState.StateType)
            {
                case MagnetStateType.Inactive:
                    ShowPopup("salvage-system-report-activate-success", component, user);
                    EntityUid gridId = magnetTransform.GridUid ?? throw new InvalidOperationException("Magnet had no grid associated");
                    if (!_salvageGridStates.TryGetValue(gridId, out gridState))
                    {
                        gridState = new SalvageGridState();
                        _salvageGridStates[gridId] = gridState;
                    }
                    gridState.ActiveMagnets.Add(component);
                    component.MagnetState = new MagnetState(MagnetStateType.Attaching, gridState.CurrentTime + _attachingTime);
                    RaiseLocalEvent(new SalvageMagnetActivatedEvent(component.Owner));
                    Report(component.Owner, component.SalvageChannel, "salvage-system-report-activate-success");
                    break;
                case MagnetStateType.Attaching:
                //case MagnetStateType.Holding:
                    ShowPopup("salvage-system-report-already-active", component, user);
                    break;
                case MagnetStateType.Holding:
                    if (magnetTransform.GridUid is EntityUid gridId2 && _salvageGridStates.TryGetValue(gridId2, out gridState))
                    {
                        // forse transition to the Detaching state
                        Transition(component, gridState.CurrentTime);
                    }
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

            if (salvageTransform.GridUid == null)
            {
                Logger.ErrorS("salvage", "Salvage entity has no associated grid?");
                return;
            }

            var parentTransform = salvageTransform.Parent!;
            foreach (var player in Filter.Empty().AddInGrid(salvageTransform.GridUid.Value, EntityManager).Recipients)
            {
                if (player.AttachedEntity.HasValue)
                {
                    var playerEntityUid = player.AttachedEntity.Value;
                    if (HasComp<SalvageMobRestrictionsComponent>(playerEntityUid))
                    {
                        // Salvage mobs are NEVER immune (even if they're from a different salvage, they shouldn't be here)
                        continue;
                    }
                    Transform(playerEntityUid).AttachParent(parentTransform);
                }
            }

            // Deletion has to happen before grid traversal re-parents players.
            EntityManager.DeleteEntity(salvage);
        }

        private void TryGetSalvagePlacementLocation(SalvageMagnetComponent component, out MapCoordinates coords, out Angle angle)
        {
            coords = MapCoordinates.Nullspace;
            angle = Angle.Zero;
            var tsc = Transform(component.Owner);
            coords = new EntityCoordinates(component.Owner, component.Offset).ToMap(EntityManager);

            if (_mapManager.TryGetGrid(tsc.GridUid, out var magnetGrid) && TryComp<TransformComponent>(magnetGrid.GridEntityId, out var gridXform))
            {
                angle = gridXform.WorldRotation;
            }
        }

        private IEnumerable<SalvageMapPrototype> GetAllSalvageMaps() =>
            _prototypeManager.EnumeratePrototypes<SalvageMapPrototype>();

        private bool SpawnSalvage(SalvageMagnetComponent component)
        {
            TryGetSalvagePlacementLocation(component, out var spl, out var spAngle);

            var forcedSalvage = _configurationManager.GetCVar<string>(CCVars.SalvageForced);
            List<SalvageMapPrototype> allSalvageMaps;
            if (string.IsNullOrWhiteSpace(forcedSalvage))
            {
                allSalvageMaps = GetAllSalvageMaps().ToList();
            }
            else
            {
                allSalvageMaps = new();
                if (_prototypeManager.TryIndex<SalvageMapPrototype>(forcedSalvage, out var forcedMap))
                {
                    allSalvageMaps.Add(forcedMap);
                }
                else
                {
                    Logger.ErrorS("c.s.salvage", $"Unable to get forced salvage map prototype {forcedSalvage}");
                }
            }

            SalvageMapPrototype? map = null;
            Vector2 spawnLocation = Vector2.Zero;

            for (var i = 0; i < allSalvageMaps.Count; i++)
            {
                SalvageMapPrototype attemptedMap = _random.PickAndTake(allSalvageMaps);
                for (var attempt = 0; attempt < SalvageLocationPlaceAttempts; attempt++)
                {
                    var randomRadius = _random.NextFloat(component.OffsetRadiusMin, component.OffsetRadiusMax);
                    var randomOffset = _random.NextAngle().ToWorldVec() * randomRadius;
                    spawnLocation = spl.Position + randomOffset;

                    var box2 = Box2.CenteredAround(spawnLocation + attemptedMap.Bounds.Center, attemptedMap.Bounds.Size);
                    var box2rot = new Box2Rotated(box2, spAngle, spawnLocation);

                    // This doesn't stop it from spawning on top of random things in space
                    // Might be better like this, ghosts could stop it before
                    if (!_mapManager.FindGridsIntersecting(spl.MapId, box2rot).Any())
                    {
                        map = attemptedMap;
                        break;
                    }
                }
                if (map != null)
                {
                    break;
                }
            }

            if (map == null)
            {
                Report(component.Owner, component.SalvageChannel, "salvage-system-announcement-spawn-no-debris-available");
                return false;
            }

            var opts = new MapLoadOptions
            {
                Offset = spawnLocation
            };

            var salvageEntityId = _map.LoadGrid(spl.MapId, map.MapPath.ToString(), opts);
            if (salvageEntityId == null)
            {
                Report(component.Owner, component.SalvageChannel, "salvage-system-announcement-spawn-debris-disintegrated");
                return false;
            }
            component.AttachedEntity = salvageEntityId;
            var gridcomp = EntityManager.EnsureComponent<SalvageGridComponent>(salvageEntityId.Value);
            gridcomp.SpawnerMagnet = component;

            var pulledTransform = EntityManager.GetComponent<TransformComponent>(salvageEntityId.Value);
            pulledTransform.WorldRotation = spAngle;

            if (_holdTime.TotalSeconds > 0)
            {
                Report(component.Owner, component.SalvageChannel, "salvage-system-announcement-arrived", ("timeLeft", _holdTime.TotalSeconds));
            }
            else
            {
                Report(component.Owner, component.SalvageChannel, "salvage-system-announcement-arrived-timeless");
            }
            return true;
        }

        private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
        {
            if (!TryComp<IntrinsicRadioReceiverComponent>(source, out var radio)) return;

            var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
            var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
            _radioSystem.SendRadioMessage(source, message, channel);
        }

        private void Transition(SalvageMagnetComponent magnet, TimeSpan currentTime)
        {
            switch (magnet.MagnetState.StateType)
            {
                case MagnetStateType.Attaching:
                    if (SpawnSalvage(magnet))
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.Holding, currentTime + _holdTime);
                    }
                    else
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + _cooldownTime);
                    }
                    break;
                case MagnetStateType.Holding:
                    if (_detachingTime.TotalSeconds > 0)
                    {
                        Report(magnet.Owner, magnet.SalvageChannel, "salvage-system-announcement-losing", ("timeLeft", _detachingTime.TotalSeconds));
                    }
                    else
                    {
                        Report(magnet.Owner, magnet.SalvageChannel, "salvage-system-announcement-losing-timeless");
                    }
                    magnet.MagnetState = new MagnetState(MagnetStateType.Detaching, currentTime + _detachingTime);
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
                    Report(magnet.Owner, magnet.SalvageChannel, "salvage-system-announcement-lost");
                    magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + _cooldownTime);
                    break;
                case MagnetStateType.CoolingDown:
                    magnet.MagnetState = MagnetState.Inactive;
                    break;
            }
            UpdateAppearance(magnet.Owner, magnet);
            UpdateChargeStateAppearance(magnet.Owner, currentTime, magnet);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.SalvageAttachingTime, SetAttachingTime);
            _configurationManager.UnsubValueChanged(CCVars.SalvageHoldTime, SetHoldTime);
            _configurationManager.UnsubValueChanged(CCVars.SalvageDetachingTime, SetDetachingTime);
            _configurationManager.UnsubValueChanged(CCVars.SalvageCooldownTime, SetCooldownTime);
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
                if (MetaData(gridId).EntityPaused) continue;
                state.CurrentTime += secondsPassed;

                var deleteQueue = new RemQueue<SalvageMagnetComponent>();

                foreach(var magnet in state.ActiveMagnets)
                {
                    UpdateChargeStateAppearance(magnet.Owner, state.CurrentTime, magnet);
                    if (magnet.MagnetState.Until > state.CurrentTime) continue;
                    if (magnet.MagnetState.StateType == MagnetStateType.Holding && _holdTime.TotalSeconds <= 0) continue;
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

