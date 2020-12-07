#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Server.AI;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    [UsedImplicitly]
    internal class AiSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<string, Type> _processorTypes = new();

        /// <summary>
        ///     To avoid iterating over dead AI continuously they can wake and sleep themselves when necessary.
        /// </summary>
        private readonly HashSet<AiLogicProcessor> _awakeAi = new();

        // To avoid modifying awakeAi while iterating over it.
        private readonly List<SleepAiMessage> _queuedSleepMessages = new();

        private readonly List<MobStateChangedMessage> _queuedMobStateMessages = new();

        public bool IsAwake(AiLogicProcessor processor) => _awakeAi.Contains(processor);

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepAiMessage>(HandleAiSleep);
            SubscribeLocalEvent<MobStateChangedMessage>(MobStateChanged);

            var processors = _reflectionManager.GetAllChildren<UtilityAi>();
            foreach (var processor in processors)
            {
                var att = (AiLogicProcessorAttribute) Attribute.GetCustomAttribute(processor, typeof(AiLogicProcessorAttribute))!;
                // Tests should pick this up
                DebugTools.AssertNotNull(att);
                _processorTypes.Add(att.SerializeName, processor);
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var cvarMaxUpdates = _configurationManager.GetCVar(CCVars.AIMaxUpdates);
            if (cvarMaxUpdates <= 0)
                return;

            foreach (var message in _queuedMobStateMessages)
            {
                if (!message.Entity.TryGetComponent(out AiControllerComponent? controller))
                {
                    continue;
                }

                controller.Processor?.MobStateChanged(message);
            }

            _queuedMobStateMessages.Clear();

            foreach (var message in _queuedSleepMessages)
            {
                switch (message.Sleep)
                {
                    case true:
                        if (_awakeAi.Count == cvarMaxUpdates && _awakeAi.Contains(message.Processor))
                        {
                            Logger.Warning($"Under AI limit again: {_awakeAi.Count - 1} / {cvarMaxUpdates}");
                        }
                        _awakeAi.Remove(message.Processor);
                        break;
                    case false:
                        _awakeAi.Add(message.Processor);

                        if (_awakeAi.Count > cvarMaxUpdates)
                        {
                            Logger.Warning($"AI limit exceeded: {_awakeAi.Count} / {cvarMaxUpdates}");
                        }
                        break;
                }
            }

            _queuedSleepMessages.Clear();
            var toRemove = new List<AiLogicProcessor>();
            var maxUpdates = Math.Min(_awakeAi.Count, cvarMaxUpdates);
            var count = 0;

            foreach (var processor in _awakeAi)
            {
                if (count >= maxUpdates)
                {
                    break;
                }

                if (processor.SelfEntity.Deleted)
                {
                    toRemove.Add(processor);
                    continue;
                }

                processor.Update(frameTime);
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

        /// <summary>
        ///     Will start up the controller's processor if not already done so.
        ///     Also add them to the awakeAi for updates.
        /// </summary>
        /// <param name="controller"></param>
        public void ProcessorInitialize(AiControllerComponent controller)
        {
            if (controller.Processor != null || controller.LogicName == null) return;
            controller.Processor = CreateProcessor(controller.LogicName);
            controller.Processor.SelfEntity = controller.Owner;
            controller.Processor.Setup();
            _awakeAi.Add(controller.Processor);
        }

        private UtilityAi CreateProcessor(string name)
        {
            if (_processorTypes.TryGetValue(name, out var type))
            {
                return (UtilityAi)_typeFactory.CreateInstance(type);
            }

            // processor needs to inherit AiLogicProcessor, and needs an AiLogicProcessorAttribute to define the YAML name
            throw new ArgumentException($"Processor type {name} could not be found.", nameof(name));
        }

        public bool ProcessorTypeExists(string name) => _processorTypes.ContainsKey(name);
    }
}
