using Content.Server.AI.Components;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.Configuration;

namespace Content.Server.AI.EntitySystems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class NPCSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private ISawmill _sawmill = default!;

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        private int _maxUpdates;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("npc");
            InitializeUtility();
            SubscribeLocalEvent<NPCComponent, MobStateChangedEvent>(OnMobStateChange);
            SubscribeLocalEvent<NPCComponent, ComponentInit>(OnNPCInit);
            SubscribeLocalEvent<NPCComponent, ComponentShutdown>(OnNPCShutdown);
            _configurationManager.OnValueChanged(CCVars.NPCEnabled, SetEnabled, true);
            _configurationManager.OnValueChanged(CCVars.NPCMaxUpdates, SetMaxUpdates, true);
        }

        private void SetMaxUpdates(int obj) => _maxUpdates = obj;
        private void SetEnabled(bool value) => Enabled = value;

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.NPCEnabled, SetEnabled);
            _configurationManager.UnsubValueChanged(CCVars.NPCMaxUpdates, SetMaxUpdates);
        }

        private void OnNPCInit(EntityUid uid, NPCComponent component, ComponentInit args)
        {
            WakeNPC(component);
        }

        private void OnNPCShutdown(EntityUid uid, NPCComponent component, ComponentShutdown args)
        {
            SleepNPC(component);
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
        public void WakeNPC(NPCComponent component)
        {
            _sawmill.Debug($"Waking {ToPrettyString(component.Owner)}");
            EnsureComp<ActiveNPCComponent>(component.Owner);
        }

        /// <summary>
        /// Stops the NPC from actively being updated.
        /// </summary>
        public void SleepNPC(NPCComponent component)
        {
            _sawmill.Debug($"Sleeping {ToPrettyString(component.Owner)}");
            RemComp<ActiveNPCComponent>(component.Owner);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            if (!Enabled) return;

            UpdateUtility();
            var count = 0;

            foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, NPCComponent>())
            {
                if (count >= _maxUpdates) break;

                Update(comp, frameTime);
                count++;
            }
        }

        private void Update(NPCComponent component, float frameTime) {}

        private void OnMobStateChange(EntityUid uid, NPCComponent component, MobStateChangedEvent args)
        {
            switch (args.CurrentMobState)
            {
                case DamageState.Alive:
                    WakeNPC(component);
                    break;
                case DamageState.Critical:
                case DamageState.Dead:
                    SleepNPC(component);
                    break;
            }
        }
    }
}
