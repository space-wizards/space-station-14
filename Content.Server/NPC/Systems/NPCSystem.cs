using Content.Server.MobState;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.NPC.Systems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class NPCSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        private ISawmill _sawmill = default!;

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

            _sawmill = Logger.GetSawmill("npc");
            _sawmill.Level = LogLevel.Info;
            SubscribeLocalEvent<NPCComponent, MobStateChangedEvent>(OnMobStateChange);
            SubscribeLocalEvent<NPCComponent, MapInitEvent>(OnNPCMapInit);
            SubscribeLocalEvent<NPCComponent, ComponentShutdown>(OnNPCShutdown);
            SubscribeLocalEvent<NPCComponent, PlayerAttachedEvent>(OnPlayerNPCAttach);
            SubscribeLocalEvent<NPCComponent, PlayerDetachedEvent>(OnPlayerNPCDetach);
            _configurationManager.OnValueChanged(CCVars.NPCEnabled, SetEnabled, true);
            _configurationManager.OnValueChanged(CCVars.NPCMaxUpdates, SetMaxUpdates, true);
        }

        private void OnPlayerNPCAttach(EntityUid uid, NPCComponent component, PlayerAttachedEvent args)
        {
            SleepNPC(uid, component);
        }

        private void OnPlayerNPCDetach(EntityUid uid, NPCComponent component, PlayerDetachedEvent args)
        {
            if (_mobState.IsIncapacitated(uid))
                return;

            WakeNPC(uid, component);
        }

        private void SetMaxUpdates(int obj) => _maxUpdates = obj;
        private void SetEnabled(bool value) => Enabled = value;

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.NPCEnabled, SetEnabled);
            _configurationManager.UnsubValueChanged(CCVars.NPCMaxUpdates, SetMaxUpdates);
        }

        private void OnNPCMapInit(EntityUid uid, NPCComponent component, MapInitEvent args)
        {
            component.Blackboard.SetValue(NPCBlackboard.Owner, uid);
            WakeNPC(uid, component);
        }

        private void OnNPCShutdown(EntityUid uid, NPCComponent component, ComponentShutdown args)
        {
            SleepNPC(uid, component);
        }

        /// <summary>
        /// Is the NPC awake and updating?
        /// </summary>
        public bool IsAwake(NPCComponent component, ActiveNPCComponent? active = null)
        {
            return Resolve(component.Owner, ref active, false);
        }

        /// <summary>
        /// Allows the NPC to actively be updated.
        /// </summary>
        public void WakeNPC(EntityUid uid, NPCComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            _sawmill.Debug($"Waking {ToPrettyString(component.Owner)}");
            EnsureComp<ActiveNPCComponent>(component.Owner);
        }

        public void SleepNPC(EntityUid uid, NPCComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            _sawmill.Debug($"Sleeping {ToPrettyString(component.Owner)}");
            RemComp<ActiveNPCComponent>(component.Owner);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Enabled)
                return;

            _count = 0;
            // Add your system here.
            _htn.UpdateNPC(ref _count, _maxUpdates, frameTime);
        }

        private void OnMobStateChange(EntityUid uid, NPCComponent component, MobStateChangedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            switch (args.CurrentMobState)
            {
                case DamageState.Alive:
                    WakeNPC(uid, component);
                    break;
                case DamageState.Critical:
                case DamageState.Dead:
                    SleepNPC(uid, component);
                    break;
            }
        }
    }
}
