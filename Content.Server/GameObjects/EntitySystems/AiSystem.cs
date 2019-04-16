using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Movement;
using Robust.Server.AI;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class AiSystem : EntitySystem
    {
        private readonly Dictionary<string, Type> _processorTypes = new Dictionary<string, Type>();
        private IPauseManager _pauseManager;

        public AiSystem()
        {
            // register entity query
            EntityQuery = new TypeEntityQuery(typeof(AiControllerComponent));
            _pauseManager = IoCManager.Resolve<IPauseManager>();

            var reflectionMan = IoCManager.Resolve<IReflectionManager>();
            var processors = reflectionMan.GetAllChildren<AiLogicProcessor>();
            foreach (var processor in processors)
            {
                var att = (AiLogicProcessorAttribute)Attribute.GetCustomAttribute(processor, typeof(AiLogicProcessorAttribute));
                if (att != null)
                {
                    _processorTypes.Add(att.SerializeName, processor);
                }
            }
        }

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
                if (aiComp.Processor == null)
                {
                    aiComp.Processor = CreateProcessor(aiComp.LogicName);
                    aiComp.Processor.SelfEntity = entity;
                    aiComp.Processor.VisionRadius = aiComp.VisionRadius;
                }

                var processor = aiComp.Processor;

                processor.Update(frameTime);
            }
        }

        private AiLogicProcessor CreateProcessor(string name)
        {
            if (_processorTypes.TryGetValue(name, out var type))
            {
                return (AiLogicProcessor)Activator.CreateInstance(type);
            }

            // processor needs to inherit AiLogicProcessor, and needs an AiLogicProcessorAttribute to define the YAML name
            throw new ArgumentException($"Processor type {name} could not be found.", nameof(name));
        }
    }
}
