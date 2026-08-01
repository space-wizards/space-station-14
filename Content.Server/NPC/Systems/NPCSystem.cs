using System.Diagnostics.CodeAnalysis;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Systems;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    public sealed partial class NPCSystem : EntitySystem
    {
        private static readonly Gauge ActiveGauge = Metrics.CreateGauge(
            "npc_active_count",
            "Amount of NPCs that are actively processing");

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly NPCSteeringSystem _steering = default!;

        // DS14-Start: put idle NPCs to sleep when no players are nearby.
        private const float ProximitySleepMinRange = 24f;
        private const float ProximitySleepRangeBuffer = 8f;
        private const float ProximitySleepMaxRange = 80f;
        private static readonly TimeSpan ProximitySleepScanInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan ProximitySleepGrace = TimeSpan.FromSeconds(15);

        private EntityQuery<GhostComponent> _ghostQuery;
        private readonly List<EntityCoordinates> _playerCoordinates = new();
        private readonly Dictionary<EntityUid, TimeSpan> _lastPlayerNearby = new();
        private readonly HashSet<EntityUid> _proximitySleeping = new();
        private TimeSpan _nextProximitySleepScan;
        // DS14-End

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        private int _maxUpdates;

        private int _count;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configurationManager, CCVars.NPCEnabled, value => Enabled = value, true);
            Subs.CVar(_configurationManager, CCVars.NPCMaxUpdates, obj => _maxUpdates = obj, true);

            // DS14-Start: put idle NPCs to sleep when no players are nearby.
            _ghostQuery = GetEntityQuery<GhostComponent>();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
            // DS14-End
        }

        public void OnPlayerNPCAttach(EntityUid uid, HTNComponent component, PlayerAttachedEvent args)
        {
            SleepNPC(uid, component);
        }

        public void OnPlayerNPCDetach(EntityUid uid, HTNComponent component, PlayerDetachedEvent args)
        {
            if (_mobState.IsIncapacitated(uid) || TerminatingOrDeleted(uid))
                return;

            // This NPC has an attached mind, so it should not wake up.
            if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
                return;

            WakeNPC(uid, component);
        }

        public void OnNPCStartup(EntityUid uid, HTNComponent component, ComponentStartup args)
        {
            component.Blackboard.SetValue(NPCBlackboard.Owner, uid);
        }

        public void OnNPCMapInit(EntityUid uid, HTNComponent component, MapInitEvent args)
        {
            WakeNPC(uid, component);
        }

        public void OnNPCShutdown(EntityUid uid, HTNComponent component, ComponentShutdown args)
        {
            SleepNPC(uid, component);

            // DS14-start: proximity state must not retain deleted NPCs.
            _lastPlayerNearby.Remove(uid);
            _proximitySleeping.Remove(uid);
            // DS14-end
        }

        /// <summary>
        /// Is the NPC awake and updating?
        /// </summary>
        public bool IsAwake(EntityUid uid, HTNComponent component, ActiveNPCComponent? active = null)
        {
            return Resolve(uid, ref active, false);
        }

        public bool TryGetNpc(EntityUid uid, [NotNullWhen(true)] out NPCComponent? component)
        {
            // If you add your own NPC components then add them here.

            if (TryComp<HTNComponent>(uid, out var htn))
            {
                component = htn;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Allows the NPC to actively be updated.
        /// </summary>
        public void WakeNPC(EntityUid uid, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            // DS14-Start: manual wake overrides proximity sleep.
            _proximitySleeping.Remove(uid);
            // DS14-End

            Log.Debug($"Waking {ToPrettyString(uid)}");
            EnsureComp<ActiveNPCComponent>(uid);
        }

        public void SleepNPC(EntityUid uid, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            _htn.CancelPlanning(component); // DS14: sleeping NPCs must not keep consuming the planning queue.

            // Don't bother with an event
            if (TryComp<HTNComponent>(uid, out var htn))
            {
                if (htn.Plan != null)
                {
                    var currentOperator = htn.Plan.CurrentOperator;
                    _htn.ShutdownTask(currentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
                    _htn.ShutdownPlan(htn);
                    htn.Plan = null;
                }
            }

            Log.Debug($"Sleeping {ToPrettyString(uid)}");
            RemComp<ActiveNPCComponent>(uid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Enabled)
                return;

            // DS14-Start: put idle NPCs to sleep when no players are nearby.
            UpdateProximitySleep();
            // DS14-End

            // Add your system here.
            _htn.UpdateNPC(ref _count, _maxUpdates, frameTime);

            ActiveGauge.Set(Count<ActiveNPCComponent>());
        }

        // DS14-Start: put idle NPCs to sleep when no players are nearby.
        private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
        {
            _playerCoordinates.Clear();
            _lastPlayerNearby.Clear();
            _proximitySleeping.Clear();
            _nextProximitySleepScan = TimeSpan.Zero;
            _count = 0;
        }

        private void UpdateProximitySleep()
        {
            var curTime = _timing.CurTime;
            if (curTime < _nextProximitySleepScan)
                return;

            _nextProximitySleepScan = curTime + ProximitySleepScanInterval;

            CachePlayerCoordinates();

            var query = EntityQueryEnumerator<HTNComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var component, out var xform))
            {
                if (!component.Enabled ||
                    HasComp<ActorComponent>(uid) ||
                    TerminatingOrDeleted(uid))
                {
                    continue;
                }

                if (_mobState.IsIncapacitated(uid))
                {
                    _lastPlayerNearby.Remove(uid);
                    continue;
                }

                if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
                    continue;

                var active = HasComp<ActiveNPCComponent>(uid);
                var nearPlayer = HasNearbyPlayer(xform, GetProximitySleepRange(component));

                if (nearPlayer)
                {
                    _lastPlayerNearby[uid] = curTime;

                    if (!active && _proximitySleeping.Contains(uid))
                    {
                        component.PlanAccumulator = 0f;
                        WakeNPC(uid, component);
                    }

                    continue;
                }

                if (!_lastPlayerNearby.TryGetValue(uid, out var lastSeen))
                {
                    _lastPlayerNearby[uid] = curTime;
                    continue;
                }

                if (!active || curTime - lastSeen < ProximitySleepGrace)
                    continue;

                _steering.Unregister(uid);
                _proximitySleeping.Add(uid);
                SleepNPC(uid, component);
            }
        }

        private void CachePlayerCoordinates()
        {
            _playerCoordinates.Clear();

            var players = EntityQueryEnumerator<ActorComponent, TransformComponent>();
            while (players.MoveNext(out var uid, out _, out var xform))
            {
                if (_ghostQuery.HasComp(uid))
                    continue;

                _playerCoordinates.Add(xform.Coordinates);
            }
        }

        private bool HasNearbyPlayer(TransformComponent xform, float range)
        {
            foreach (var coordinates in _playerCoordinates)
            {
                if (xform.Coordinates.TryDistance(EntityManager, coordinates, out var distance) &&
                    distance <= range)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetProximitySleepRange(HTNComponent component)
        {
            var radius = component.Blackboard.GetValueOrDefault<float>(
                component.Blackboard.GetVisionRadiusKey(EntityManager),
                EntityManager);

            return Math.Clamp(radius + ProximitySleepRangeBuffer, ProximitySleepMinRange, ProximitySleepMaxRange);
        }
        // DS14-End

        public void OnMobStateChange(EntityUid uid, HTNComponent component, MobStateChangedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                    WakeNPC(uid, component);
                    break;
                case MobState.Critical:
                case MobState.Dead:
                    SleepNPC(uid, component);
                    break;
            }
        }
    }
}
