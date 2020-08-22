using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Server.AI;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    [UsedImplicitly]
    internal class AiSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
        [Dependency] private readonly IReflectionManager _reflectionManager;
#pragma warning restore 649

        private readonly Dictionary<string, Type> _processorTypes = new Dictionary<string, Type>();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // register entity query
            EntityQuery = new TypeEntityQuery(typeof(AiControllerComponent));

            var processors = _reflectionManager.GetAllChildren<AiLogicProcessor>();
            foreach (var processor in processors)
            {
                var att = (AiLogicProcessorAttribute)Attribute.GetCustomAttribute(processor, typeof(AiLogicProcessorAttribute));
                // Tests should pick this up
                DebugTools.AssertNotNull(att);
                _processorTypes.Add(att.SerializeName, processor);
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }

                var aiComp = entity.GetComponent<AiControllerComponent>();
                ProcessorInitialize(aiComp);

                var processor = aiComp.Processor;

                processor.Update(frameTime);
            }
        }

        /// <summary>
        /// Will start up the controller's processor if not already done so
        /// </summary>
        /// <param name="controller"></param>
        public void ProcessorInitialize(AiControllerComponent controller)
        {
            if (controller.Processor != null) return;
            controller.Processor = CreateProcessor(controller.LogicName);
            controller.Processor.SelfEntity = controller.Owner;
            controller.Processor.Setup();
        }

        private AiLogicProcessor CreateProcessor(string name)
        {
            if (_processorTypes.TryGetValue(name, out var type))
            {
                return (AiLogicProcessor)_typeFactory.CreateInstance(type);
            }

            // processor needs to inherit AiLogicProcessor, and needs an AiLogicProcessorAttribute to define the YAML name
            throw new ArgumentException($"Processor type {name} could not be found.", nameof(name));
        }

        public bool ProcessorTypeExists(string name) => _processorTypes.ContainsKey(name);


        private class AddAiCommand : IClientCommand
        {
            public string Command => "addai";
            public string Description => "Add an ai component with a given processor to an entity.";
            public string Help => "Usage: addai <processorId> <entityId>"
                                + "\n    processorId: Class that inherits AiLogicProcessor and has an AiLogicProcessor attribute."
                                + "\n    entityID: Uid of entity to add the AiControllerComponent to. Open its VV menu to find this.";

            public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
            {
                if(args.Length != 2)
                {
                    shell.SendText(player, "Wrong number of args.");
                    return;
                }

                var processorId = args[0];
                var entId = new EntityUid(int.Parse(args[1]));
                var ent = IoCManager.Resolve<IEntityManager>().GetEntity(entId);
                var aiSystem = EntitySystem.Get<AiSystem>();

                if (!aiSystem.ProcessorTypeExists(processorId))
                {
                    shell.SendText(player, "Invalid processor type. Processor must inherit AiLogicProcessor and have an AiLogicProcessor attribute.");
                    return;
                }
                if (ent.HasComponent<AiControllerComponent>())
                {
                    shell.SendText(player, "Entity already has an AI component.");
                    return;
                }

                if (ent.HasComponent<IMoverComponent>())
                {
                    ent.RemoveComponent<IMoverComponent>();
                }

                var comp = ent.AddComponent<AiControllerComponent>();
                comp.LogicName = processorId;
                shell.SendText(player, "AI component added.");
            }
        }
    }
}
