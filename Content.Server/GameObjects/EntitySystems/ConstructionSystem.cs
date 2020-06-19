using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : Shared.GameObjects.EntitySystems.ConstructionSystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        private readonly Dictionary<string, ConstructionPrototype> _craftRecipes = new Dictionary<string, ConstructionPrototype>();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<ConstructionPrototype>())
            {
                _craftRecipes.Add(prototype.Result, prototype);
            }

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);

            SubscribeLocalEvent<AfterAttackMessage>(HandleToolInteraction);
        }

        private void HandleStartStructureConstruction(TryStartStructureConstructionMessage msg, EntitySessionEventArgs args)
        {
            var placingEnt = args.SenderSession.AttachedEntity;
            var result = TryStartStructureConstruction(placingEnt, msg.Location, msg.PrototypeName, msg.Angle);
            if (!result) return;
            var responseMsg = new AckStructureConstructionMessage(msg.Ack);
            var channel = ((IPlayerSession) args.SenderSession).ConnectedClient;
            RaiseNetworkEvent(responseMsg, channel);
        }

        private void HandleStartItemConstruction(TryStartItemConstructionMessage msg, EntitySessionEventArgs args)
        {
            var placingEnt = args.SenderSession.AttachedEntity;
            TryStartItemConstruction(placingEnt, msg.PrototypeName);
        }

        private void HandleToolInteraction(AfterAttackMessage msg)
        {
            if(msg.Handled)
                return;

            var targetEnt = msg.Attacked;
            var handEnt = msg.ItemInHand;

            // A tool has to interact with an entity.
            if(targetEnt is null || handEnt is null)
                return;

            // A tool was not used on the entity.
            if (!handEnt.TryGetComponent<IToolComponent>(out var toolComp))
                return;

            // Cannot deconstruct an entity with no prototype.
            var targetPrototype = targetEnt.MetaData.EntityPrototype;
            if (targetPrototype is null)
                return;

            // the target entity is in the process of being constructed
            if (msg.Attacked.TryGetComponent<ConstructionComponent>(out var constructComp))
            {
                //TODO: Continue constructing
                var result = TryConstructEntity(constructComp, handEnt, msg.User);

                _notifyManager.PopupMessage(msg.Attacked, msg.User,
                    "TODO: Continue Construction.");

                msg.Handled = result;
                return;
            }
            else // try to start the deconstruction process
            {
                // no known recipe for entity
                if (!_craftRecipes.TryGetValue(targetPrototype.Name, out var prototype))
                {
                    _notifyManager.PopupMessage(msg.Attacked, msg.User,
                        "Cannot be deconstructed.");
                    return;
                }

                // there is a recipe, but you have the wrong tool

                //TODO: start the deconstruct process
                _notifyManager.PopupMessage(msg.Attacked, msg.User,
                    "TODO: Start Deconstruct.");

                return;
            }
        }

        private bool TryStartStructureConstruction(IEntity placingEnt, GridCoordinates loc, string prototypeName, Angle angle)
        {
            var prototype = _prototypeManager.Index<ConstructionPrototype>(prototypeName);

            if (!InteractionChecks.InRangeUnobstructed(placingEnt, loc.ToMap(_mapManager),
                ignoredEnt: placingEnt, insideBlockerValid: prototype.CanBuildInImpassable))
            {
                return false;
            }

            if (prototype.Stages.Count < 2)
            {
                throw new InvalidOperationException($"Prototype '{prototypeName}' does not have enough stages.");
            }

            var stage0 = prototype.Stages[0];
            if (!(stage0.Forward is ConstructionStepMaterial matStep))
            {
                throw new NotImplementedException();
            }

            // Try to find the stack with the material in the user's hand.
            var hands = placingEnt.GetComponent<HandsComponent>();
            var activeHand = hands.GetActiveHand?.Owner;
            if (activeHand == null)
            {
                return false;
            }

            if (!activeHand.TryGetComponent(out StackComponent stack) || !MaterialStackValidFor(matStep, stack))
            {
                return false;
            }

            if (!stack.Use(matStep.Amount))
            {
                return false;
            }

            // OK WE'RE GOOD CONSTRUCTION STARTED.
            Get<AudioSystem>().PlayAtCoords("/Audio/items/deconstruct.ogg", loc);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = EntityManager.SpawnEntity(prototype.Result, loc);
                ent.Transform.LocalRotation = angle;
            }
            else
            {
                var frame = EntityManager.SpawnEntity("structureconstructionframe", loc);
                var construction = frame.GetComponent<ConstructionComponent>();
                SetupComponent(construction, prototype);
                frame.Transform.LocalRotation = angle;
            }

            return true;
        }

        private void TryStartItemConstruction(IEntity placingEnt, string prototypeName)
        {
            var prototype = _prototypeManager.Index<ConstructionPrototype>(prototypeName);

            if (prototype.Stages.Count < 2)
            {
                throw new InvalidOperationException($"Prototype '{prototypeName}' does not have enough stages.");
            }

            var stage0 = prototype.Stages[0];
            if (!(stage0.Forward is ConstructionStepMaterial matStep))
            {
                throw new NotImplementedException();
            }

            // Try to find the stack with the material in the user's hand.
            var hands = placingEnt.GetComponent<HandsComponent>();
            var activeHand = hands.GetActiveHand?.Owner;
            if (activeHand == null)
            {
                return;
            }

            if (!activeHand.TryGetComponent(out StackComponent stack) || !MaterialStackValidFor(matStep, stack))
            {
                return;
            }

            if (!stack.Use(matStep.Amount))
            {
                return;
            }

            // OK WE'RE GOOD CONSTRUCTION STARTED.
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/items/deconstruct.ogg", placingEnt);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = EntityManager.SpawnEntity(prototype.Result, placingEnt.Transform.GridPosition);
                hands.PutInHandOrDrop(ent.GetComponent<ItemComponent>());
            }
            else
            {
                //TODO: Make these viable as an item and try putting them in the players hands
                var frame = EntityManager.SpawnEntity("structureconstructionframe", placingEnt.Transform.GridPosition);
                var construction = frame.GetComponent<ConstructionComponent>();
                SetupComponent(construction, prototype);
            }

        }

        private bool TryConstructEntity(ConstructionComponent constructionComponent, IEntity handTool, IEntity user)
        {
            var constructEntity = constructionComponent.Owner;
            var spriteComponent = constructEntity.GetComponent<SpriteComponent>();
            var transformComponent = constructEntity.GetComponent<ITransformComponent>();

            // default interaction check for AttackBy allows inside blockers, so we will check if its blocked if
            // we're not allowed to build on impassable stuff
            var constructPrototype = constructionComponent.Prototype;
            if (constructPrototype.CanBuildInImpassable == false)
            {
                if (!InteractionChecks.InRangeUnobstructed(user, constructEntity.Transform.MapPosition))
                    return false;
            }

            var stage = constructPrototype.Stages[constructionComponent.Stage];

            if (TryProcessStep(constructEntity, stage.Forward, handTool, user, transformComponent.GridPosition))
            {
                constructionComponent.Stage++;
                if (constructionComponent.Stage == constructPrototype.Stages.Count - 1)
                {
                    // Oh boy we get to finish construction!
                    var ent = EntityManager.SpawnEntity(constructPrototype.Result, transformComponent.GridPosition);
                    ent.Transform.LocalRotation = transformComponent.LocalRotation;
                    constructEntity.Delete();
                    return true;
                }

                stage = constructPrototype.Stages[constructionComponent.Stage];
                if (stage.Icon != null)
                {
                    spriteComponent.LayerSetSprite(0, stage.Icon);
                }
            }

            else if (TryProcessStep(constructEntity, stage.Backward, handTool, user, transformComponent.GridPosition))
            {
                constructionComponent.Stage--;
                if (constructionComponent.Stage == 0)
                {
                    // Deconstruction complete.
                    constructEntity.Delete();
                    return true;
                }

                stage = constructPrototype.Stages[constructionComponent.Stage];
                if (stage.Icon != null)
                {
                    spriteComponent.LayerSetSprite(0, stage.Icon);
                }
            }

            return true;
        }

        private bool TryProcessStep(IEntity constructEntity, ConstructionStep step, IEntity slapped, IEntity user, GridCoordinates gridCoords)
        {
            if (step == null)
            {
                return false;
            }
            
            var sound = EntitySystemManager.GetEntitySystem<AudioSystem>();

            switch (step)
            {
                case ConstructionStepMaterial matStep:
                    if (!slapped.TryGetComponent(out StackComponent stack)
                        || !MaterialStackValidFor(matStep, stack)
                        || !stack.Use(matStep.Amount))
                    {
                        return false;
                    }
                    if (matStep.Material == ConstructionStepMaterial.MaterialType.Cable)
                        sound.PlayAtCoords("/Audio/items/zip.ogg", gridCoords);
                    else
                        sound.PlayAtCoords("/Audio/items/deconstruct.ogg", gridCoords);
                    return true;
                case ConstructionStepTool toolStep:
                    if (!slapped.TryGetComponent<ToolComponent>(out var tool))
                        return false;

                    // Handle welder manually since tool steps specify fuel amount needed, for some reason.
                    if (toolStep.ToolQuality.HasFlag(ToolQuality.Welding))
                        return slapped.TryGetComponent<WelderComponent>(out var welder)
                               && welder.UseTool(user, constructEntity, toolStep.ToolQuality, toolStep.Amount);

                    return tool.UseTool(user, constructEntity, toolStep.ToolQuality);

                default:
                    throw new NotImplementedException();
            }
        }

        // Really this should check the actual materials at play..
        private static readonly Dictionary<StackType, ConstructionStepMaterial.MaterialType> StackTypeMap
            = new Dictionary<StackType, ConstructionStepMaterial.MaterialType>
            {
                { StackType.Cable, ConstructionStepMaterial.MaterialType.Cable },
                { StackType.Gold, ConstructionStepMaterial.MaterialType.Gold },
                { StackType.Glass, ConstructionStepMaterial.MaterialType.Glass },
                { StackType.Metal, ConstructionStepMaterial.MaterialType.Metal }
            };

        private static bool MaterialStackValidFor(ConstructionStepMaterial step, StackComponent stack)
        {
            return StackTypeMap.TryGetValue((StackType)stack.StackType, out var should) && should == step.Material;
        }

        private static void SetupComponent(ConstructionComponent constructionComponent, ConstructionPrototype prototype)
        {
            constructionComponent.Prototype = prototype;
            constructionComponent.Stage = 1;
            var spriteComp = constructionComponent.Owner.GetComponent<SpriteComponent>();
            if(prototype.Stages[1].Icon != null)
            {
                spriteComp.AddLayerWithSprite(prototype.Stages[1].Icon);
            }
            else
            {
                spriteComp.AddLayerWithSprite(prototype.Icon);
            }


        }
    }
}
