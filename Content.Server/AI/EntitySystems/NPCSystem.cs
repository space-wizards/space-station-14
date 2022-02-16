using System.Collections.Generic;
using System.Linq;
using Content.Server.AI.Components;
using Content.Server.MobState.States;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.AI.EntitySystems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    [UsedImplicitly]
    internal sealed class NPCSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        /// <summary>
        ///     To avoid iterating over dead AI continuously they can wake and sleep themselves when necessary.
        /// </summary>
        private readonly HashSet<AiControllerComponent> _awakeNPCs = new();

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AiControllerComponent, MobStateChangedEvent>(OnMobStateChange);
            SubscribeLocalEvent<AiControllerComponent, ComponentInit>(OnNPCInit);
            SubscribeLocalEvent<AiControllerComponent, ComponentShutdown>(OnNPCShutdown);
            _configurationManager.OnValueChanged(CCVars.NPCEnabled, SetEnabled, true);

            var maxUpdates = _configurationManager.GetCVar(CCVars.NPCMaxUpdates);

            if (maxUpdates < 1024)
                _awakeNPCs.EnsureCapacity(maxUpdates);
        }

        private void SetEnabled(bool value) => Enabled = value;

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.NPCEnabled, SetEnabled);
        }

        private void OnNPCInit(EntityUid uid, AiControllerComponent component, ComponentInit args)
        {
            if (!component.Awake) return;

            _awakeNPCs.Add(component);
        }

        private void OnNPCShutdown(EntityUid uid, AiControllerComponent component, ComponentShutdown args)
        {
            _awakeNPCs.Remove(component);
        }

        /// <summary>
        /// Allows the NPC to actively be updated.
        /// </summary>
        /// <param name="component"></param>
        public void WakeNPC(AiControllerComponent component)
        {
            _awakeNPCs.Add(component);
        }

        /// <summary>
        /// Stops the NPC from actively being updated.
        /// </summary>
        /// <param name="component"></param>
        public void SleepNPC(AiControllerComponent component)
        {
            _awakeNPCs.Remove(component);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            if (!Enabled) return;

            var cvarMaxUpdates = _configurationManager.GetCVar(CCVars.NPCMaxUpdates);

            if (cvarMaxUpdates <= 0) return;

            var npcs = _awakeNPCs.ToArray();
            var startIndex = 0;

            // If we're overcap we'll just update randomly so they all still at least do something
            // Didn't randomise the array (even though it'd probably be better) because god damn that'd be expensive.
            if (npcs.Length > cvarMaxUpdates)
            {
                startIndex = _robustRandom.Next(npcs.Length);
            }

            for (var i = 0; i < npcs.Length; i++)
            {
                MetaDataComponent? metadata = null;
                var index = (i + startIndex) % npcs.Length;
                var npc = npcs[index];

                if (Deleted(npc.Owner, metadata))
                    continue;

                // Probably gets resolved in deleted for us already
                if (Paused(npc.Owner, metadata))
                    continue;

                npc.Update(frameTime);
            }
        }

        private void OnMobStateChange(EntityUid uid, AiControllerComponent component, MobStateChangedEvent args)
        {
            switch (args.CurrentMobState)
            {
                case NormalMobState:
                    component.Awake = true;
                    break;
                case CriticalMobState:
                case DeadMobState:
                    component.Awake = false;
                    break;
            }
        }
    }
}
