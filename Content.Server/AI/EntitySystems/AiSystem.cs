#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    [UsedImplicitly]
    internal class AiSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        /// <summary>
        ///     To avoid iterating over dead AI continuously they can wake and sleep themselves when necessary.
        /// </summary>
        private readonly HashSet<AiControllerComponent> _awakeAi = new();

        // To avoid modifying awakeAi while iterating over it.
        private readonly List<SleepAiMessage> _queuedSleepMessages = new();

        private readonly List<MobStateChangedMessage> _queuedMobStateMessages = new();

        public bool IsAwake(AiControllerComponent npc) => _awakeAi.Contains(npc);

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepAiMessage>(HandleAiSleep);
            SubscribeLocalEvent<MobStateChangedMessage>(MobStateChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<SleepAiMessage>();
            UnsubscribeLocalEvent<MobStateChangedMessage>();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var cvarMaxUpdates = _configurationManager.GetCVar(CCVars.AIMaxUpdates);
            if (cvarMaxUpdates <= 0)
                return;

            foreach (var message in _queuedMobStateMessages)
            {
                // TODO: Need to generecise this but that will be part of a larger cleanup later anyway.
                if (message.Entity.Deleted ||
                    !message.Entity.TryGetComponent(out UtilityAi? controller))
                {
                    continue;
                }

                controller.MobStateChanged(message);
            }

            _queuedMobStateMessages.Clear();

            foreach (var message in _queuedSleepMessages)
            {
                switch (message.Sleep)
                {
                    case true:
                        if (_awakeAi.Count == cvarMaxUpdates && _awakeAi.Contains(message.Component))
                        {
                            Logger.Warning($"Under AI limit again: {_awakeAi.Count - 1} / {cvarMaxUpdates}");
                        }
                        _awakeAi.Remove(message.Component);
                        break;
                    case false:
                        _awakeAi.Add(message.Component);

                        if (_awakeAi.Count > cvarMaxUpdates)
                        {
                            Logger.Warning($"AI limit exceeded: {_awakeAi.Count} / {cvarMaxUpdates}");
                        }
                        break;
                }
            }

            _queuedSleepMessages.Clear();
            var toRemove = new List<AiControllerComponent>();
            var maxUpdates = Math.Min(_awakeAi.Count, cvarMaxUpdates);
            var count = 0;

            foreach (var npc in _awakeAi)
            {
                if (npc.Paused) continue;

                if (npc.Deleted)
                {
                    toRemove.Add(npc);
                    continue;
                }

                if (count >= maxUpdates)
                {
                    break;
                }

                npc.Update(frameTime);
                count++;
            }

            foreach (var processor in toRemove)
            {
                _awakeAi.Remove(processor);
            }
        }

        private void HandleAiSleep(SleepAiMessage message)
        {
            _queuedSleepMessages.Add(message);
        }

        private void MobStateChanged(MobStateChangedMessage message)
        {
            if (!message.Entity.HasComponent<AiControllerComponent>())
            {
                return;
            }

            _queuedMobStateMessages.Add(message);
        }
    }
}
