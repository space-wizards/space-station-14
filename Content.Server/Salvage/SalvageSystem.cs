using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Construction;
using Content.Server.GameTicking;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Chat.Managers;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Tools.Components;
using Robust.Server.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Salvage
{
    public sealed partial class SalvageSystem : SharedSalvageSystem
    {
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AnchorableSystem _anchorable = default!;
        [Dependency] private readonly BiomeSystem _biome = default!;
        [Dependency] private readonly DungeonSystem _dungeon = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly ShuttleConsoleSystem _shuttleConsoles = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        private const int SalvageLocationPlaceAttempts = 25;

        // TODO: This is probably not compatible with multi-station
        private readonly Dictionary<EntityUid, SalvageGridState> _salvageGridStates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SalvageMagnetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SalvageMagnetComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<SalvageMagnetComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<SalvageMagnetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SalvageMagnetComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
            SubscribeLocalEvent<SalvageMagnetComponent, ComponentShutdown>(OnMagnetRemoval);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

            // Can't use RoundRestartCleanupEvent, I need to clean up before the grid, and components are gone to prevent the announcements
            SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);

            InitializeExpeditions();
            InitializeRunner();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownExpeditions();
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

            var timeLeft = Convert.ToInt32(component.MagnetState.Until.TotalSeconds - currentTime.TotalSeconds);

            component.ChargeRemaining = component.MagnetState.StateType switch
            {
                MagnetStateType.Inactive => 5,
                MagnetStateType.Holding => timeLeft / (Convert.ToInt32(component.HoldTime.TotalSeconds) / component.ChargeCapacity) + 1,
                MagnetStateType.Detaching => 0,
                MagnetStateType.CoolingDown => component.ChargeCapacity - timeLeft / (Convert.ToInt32(component.CooldownTime.TotalSeconds) / component.ChargeCapacity) - 1,
                _ => component.ChargeRemaining
            };

            if (component.PreviousCharge == component.ChargeRemaining)
                return;
            _appearanceSystem.SetData(uid, SalvageMagnetVisuals.ChargeState, component.ChargeRemaining);
            component.PreviousCharge = component.ChargeRemaining;
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            // If we ever want to give magnets names, and announce them individually, we would need to loop this, before removing it.
            if (_salvageGridStates.Remove(ev.EntityUid))
            {
                if (TryComp<SalvageGridComponent>(ev.EntityUid, out var salvComp) &&
                    TryComp<SalvageMagnetComponent>(salvComp.SpawnerMagnet, out var magnet))
                    Report(salvComp.SpawnerMagnet.Value, magnet.SalvageChannel, "salvage-system-announcement-spawn-magnet-lost");
                // For the very unlikely possibility that the salvage magnet was on a salvage, we will not return here
            }
            foreach(var gridState in _salvageGridStates)
            {
                foreach(var magnet in gridState.Value.ActiveMagnets)
                {
                    if (!TryComp<SalvageMagnetComponent>(magnet, out var magnetComponent))
                        continue;

                    if (magnetComponent.AttachedEntity != ev.EntityUid)
                        continue;
                    magnetComponent.AttachedEntity = null;
                    magnetComponent.MagnetState = MagnetState.Inactive;
                    return;
                }
            }
        }

        private void OnMagnetRemoval(EntityUid uid, SalvageMagnetComponent component, ComponentShutdown args)
        {
            if (component.MagnetState.StateType == MagnetStateType.Inactive)
                return;

            var magnetTranform = Transform(uid);
            if (magnetTranform.GridUid is not { } gridId || !_salvageGridStates.TryGetValue(gridId, out var salvageGridState))
                return;

            salvageGridState.ActiveMagnets.Remove(uid);
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

        private void OnRefreshParts(EntityUid uid, SalvageMagnetComponent component, RefreshPartsEvent args)
        {
            var rating = args.PartRatings[component.MachinePartDelay] - 1;
            var factor = MathF.Pow(component.PartRatingDelay, rating);
            component.AttachingTime = component.BaseAttachingTime * factor;
            component.CooldownTime = component.BaseCooldownTime * factor;
        }

        private void OnUpgradeExamine(EntityUid uid, SalvageMagnetComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("salvage-system-magnet-delay-upgrade", (float) (component.CooldownTime / component.BaseCooldownTime));
        }

        private void OnExamined(EntityUid uid, SalvageMagnetComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            var gotGrid = false;
            var remainingTime = TimeSpan.Zero;

            if (Transform(uid).GridUid is { } gridId &&
                _salvageGridStates.TryGetValue(gridId, out var salvageGridState))
            {
                remainingTime = component.MagnetState.Until - salvageGridState.CurrentTime;
                gotGrid = true;
            }
            else
            {
                Log.Warning("Failed to load salvage grid state, can't display remaining time");
            }

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
                    if (gotGrid)
                        args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-cooling-down", ("timeLeft", Math.Ceiling(remainingTime.TotalSeconds))));
                    break;
                case MagnetStateType.Holding:
                    if (gotGrid)
                        args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-active", ("timeLeft", Math.Ceiling(remainingTime.TotalSeconds))));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnToolUseAttempt(EntityUid uid, SalvageMagnetComponent comp, ToolUseAttemptEvent args)
        {
            // prevent reconstruct exploit to "leak" wrecks or skip cooldowns
            if (comp.MagnetState != MagnetState.Inactive)
            {
                args.Cancel();
            }
        }

        private void OnInteractHand(EntityUid uid, SalvageMagnetComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = true;
            StartMagnet(uid, component, args.User);
            UpdateAppearance(uid, component);
        }

        private void StartMagnet(EntityUid uid, SalvageMagnetComponent component, EntityUid user)
        {
            switch (component.MagnetState.StateType)
            {
                case MagnetStateType.Inactive:
                    ShowPopup(uid, "salvage-system-report-activate-success", user);
                    var magnetTransform = Transform(uid);
                    var gridId = magnetTransform.GridUid ?? throw new InvalidOperationException("Magnet had no grid associated");
                    if (!_salvageGridStates.TryGetValue(gridId, out var gridState))
                    {
                        gridState = new SalvageGridState();
                        _salvageGridStates[gridId] = gridState;
                    }
                    gridState.ActiveMagnets.Add(uid);
                    component.MagnetState = new MagnetState(MagnetStateType.Attaching, gridState.CurrentTime + component.AttachingTime);
                    RaiseLocalEvent(new SalvageMagnetActivatedEvent(uid));
                    Report(uid, component.SalvageChannel, "salvage-system-report-activate-success");
                    break;
                case MagnetStateType.Attaching:
                case MagnetStateType.Holding:
                    ShowPopup(uid, "salvage-system-report-already-active", user);
                    break;
                case MagnetStateType.Detaching:
                case MagnetStateType.CoolingDown:
                    ShowPopup(uid, "salvage-system-report-cooling-down", user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void ShowPopup(EntityUid uid, string messageKey, EntityUid user)
        {
            _popupSystem.PopupEntity(Loc.GetString(messageKey), uid, user);
        }

        private void SafeDeleteSalvage(EntityUid salvage)
        {
            if(!EntityManager.TryGetComponent<TransformComponent>(salvage, out var salvageTransform))
            {
                Log.Error("Salvage entity was missing transform component");
                return;
            }

            if (salvageTransform.GridUid == null)
            {
                Log.Error( "Salvage entity has no associated grid?");
                return;
            }

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
                    _transform.SetParent(playerEntityUid, salvageTransform.ParentUid);
                }
            }

            // Deletion has to happen before grid traversal re-parents players.
            Del(salvage);
        }

        private bool TryGetSalvagePlacementLocation(EntityUid uid, SalvageMagnetComponent component, Box2 bounds, out MapCoordinates coords, out Angle angle)
        {
            var xform = Transform(uid);
            var smallestBound = (bounds.Height < bounds.Width
                ? bounds.Height
                : bounds.Width) / 2f;
            var maxRadius = component.OffsetRadiusMax + smallestBound;

            angle = Angle.Zero;
            coords = new EntityCoordinates(uid, new Vector2(0, -maxRadius)).ToMap(EntityManager, _transform);

            if (xform.GridUid is not null)
                angle = _transform.GetWorldRotation(Transform(xform.GridUid.Value));

            for (var i = 0; i < SalvageLocationPlaceAttempts; i++)
            {
                var randomRadius = _random.NextFloat(component.OffsetRadiusMax);
                var randomOffset = _random.NextAngle().ToVec() * randomRadius;
                var finalCoords = new MapCoordinates(coords.Position + randomOffset, coords.MapId);

                var box2 = Box2.CenteredAround(finalCoords.Position, bounds.Size);
                var box2Rot = new Box2Rotated(box2, angle, finalCoords.Position);

                // This doesn't stop it from spawning on top of random things in space
                // Might be better like this, ghosts could stop it before
                if (_mapManager.FindGridsIntersecting(finalCoords.MapId, box2Rot).Any())
                    continue;
                coords = finalCoords;
                return true;
            }
            return false;
        }

        private bool SpawnSalvage(EntityUid uid, SalvageMagnetComponent component)
        {
            var salvMap = _mapManager.CreateMap();

            EntityUid? salvageEnt;
            if (_random.Prob(component.AsteroidChance))
            {
                var asteroidProto = _prototypeManager.Index<WeightedRandomEntityPrototype>(component.AsteroidPool).Pick(_random);
                salvageEnt = Spawn(asteroidProto, new MapCoordinates(0, 0, salvMap));
            }
            else
            {
                var forcedSalvage = _configurationManager.GetCVar(CCVars.SalvageForced);
                var salvageProto = string.IsNullOrWhiteSpace(forcedSalvage)
                    ? _random.Pick(_prototypeManager.EnumeratePrototypes<SalvageMapPrototype>().ToList())
                    : _prototypeManager.Index<SalvageMapPrototype>(forcedSalvage);

                var opts = new MapLoadOptions
                {
                    Offset = new Vector2(0, 0)
                };

                if (!_map.TryLoad(salvMap, salvageProto.MapPath.ToString(), out var roots, opts) ||
                    roots.FirstOrNull() is not { } root)
                {
                    Report(uid, component.SalvageChannel, "salvage-system-announcement-spawn-debris-disintegrated");
                    _mapManager.DeleteMap(salvMap);
                    return false;
                }

                salvageEnt = root;
            }

            var bounds = Comp<MapGridComponent>(salvageEnt.Value).LocalAABB;
            if (!TryGetSalvagePlacementLocation(uid, component, bounds, out var spawnLocation, out var spawnAngle))
            {
                Report(uid, component.SalvageChannel, "salvage-system-announcement-spawn-no-debris-available");
                _mapManager.DeleteMap(salvMap);
                return false;
            }

            var salvXForm = Transform(salvageEnt.Value);
            _transform.SetParent(salvageEnt.Value, salvXForm, _mapManager.GetMapEntityId(spawnLocation.MapId));
            _transform.SetWorldPosition(salvXForm, spawnLocation.Position);

            component.AttachedEntity = salvageEnt;
            var gridcomp = EnsureComp<SalvageGridComponent>(salvageEnt.Value);
            gridcomp.SpawnerMagnet = uid;
            _transform.SetWorldRotation(salvageEnt.Value, spawnAngle);

            Report(uid, component.SalvageChannel, "salvage-system-announcement-arrived", ("timeLeft", component.HoldTime.TotalSeconds));
            _mapManager.DeleteMap(salvMap);
            return true;
        }

        private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
        {
            var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
            var channel = _prototypeManager.Index<RadioChannelPrototype>(channelName);
            _radioSystem.SendRadioMessage(source, message, channel, source);
        }

        private void Transition(EntityUid uid, SalvageMagnetComponent magnet, TimeSpan currentTime)
        {
            switch (magnet.MagnetState.StateType)
            {
                case MagnetStateType.Attaching:
                    if (SpawnSalvage(uid, magnet))
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.Holding, currentTime + magnet.HoldTime);
                    }
                    else
                    {
                        magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + magnet.CooldownTime);
                    }
                    break;
                case MagnetStateType.Holding:
                    Report(uid, magnet.SalvageChannel, "salvage-system-announcement-losing", ("timeLeft", magnet.DetachingTime.TotalSeconds));
                    magnet.MagnetState = new MagnetState(MagnetStateType.Detaching, currentTime + magnet.DetachingTime);
                    break;
                case MagnetStateType.Detaching:
                    if (magnet.AttachedEntity.HasValue)
                    {
                        SafeDeleteSalvage(magnet.AttachedEntity.Value);
                    }
                    else
                    {
                        Log.Error("Salvage detaching was expecting attached entity but it was null");
                    }
                    Report(uid, magnet.SalvageChannel, "salvage-system-announcement-lost");
                    magnet.MagnetState = new MagnetState(MagnetStateType.CoolingDown, currentTime + magnet.CooldownTime);
                    break;
                case MagnetStateType.CoolingDown:
                    magnet.MagnetState = MagnetState.Inactive;
                    break;
            }
            UpdateAppearance(uid, magnet);
            UpdateChargeStateAppearance(uid, currentTime, magnet);
        }

        public override void Update(float frameTime)
        {
            var secondsPassed = TimeSpan.FromSeconds(frameTime);
            // Keep track of time, and state per grid
            foreach (var (uid, state) in _salvageGridStates)
            {
                if (state.ActiveMagnets.Count == 0) continue;
                // Not handling the case where the salvage we spawned got paused
                // They both need to be paused, or it doesn't make sense
                if (MetaData(uid).EntityPaused) continue;
                state.CurrentTime += secondsPassed;

                var deleteQueue = new RemQueue<EntityUid>();

                foreach(var magnet in state.ActiveMagnets)
                {
                    if (!TryComp<SalvageMagnetComponent>(magnet, out var magnetComp))
                        continue;

                    UpdateChargeStateAppearance(magnet, state.CurrentTime, magnetComp);
                    if (magnetComp.MagnetState.Until > state.CurrentTime) continue;
                    Transition(magnet, magnetComp, state.CurrentTime);
                    if (magnetComp.MagnetState.StateType == MagnetStateType.Inactive)
                    {
                        deleteQueue.Add(magnet);
                    }
                }

                foreach(var magnet in deleteQueue)
                {
                    state.ActiveMagnets.Remove(magnet);
                }
            }

            UpdateExpeditions();
            UpdateRunner();
        }
    }

    public sealed class SalvageGridState
    {
        public TimeSpan CurrentTime { get; set; }
        public List<EntityUid> ActiveMagnets { get; } = new();
    }
}

