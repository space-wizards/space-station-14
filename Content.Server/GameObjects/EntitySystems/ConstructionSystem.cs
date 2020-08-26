using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Utility;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
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
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<string, ConstructionPrototype> _craftRecipes = new Dictionary<string, ConstructionPrototype>();

        public IReadOnlyDictionary<string, ConstructionPrototype> CraftRecipes => _craftRecipes;

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

            SubscribeLocalEvent<AfterInteractMessage>(HandleToolInteraction);
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

        private async void HandleToolInteraction(AfterInteractMessage msg)
        {
            if(msg.Handled)
                return;

            // You can only construct/deconstruct things within reach
            if(!msg.CanReach)
                return;

            var targetEnt = msg.Attacked;
            var handEnt = msg.ItemInHand;

            // A tool has to interact with an entity.
            if(targetEnt is null || handEnt is null)
                return;

            var interaction = Get<InteractionSystem>();
            if(!interaction.InRangeUnobstructed(handEnt.Transform.MapPosition, targetEnt.Transform.MapPosition, ignoredEnt: targetEnt, ignoreInsideBlocker: true))
                return;

            // Cannot deconstruct an entity with no prototype.
            var targetPrototype = targetEnt.MetaData.EntityPrototype;
            if (targetPrototype is null)
                return;

            // the target entity is in the process of being constructed/deconstructed
            if (msg.Attacked.TryGetComponent<ConstructionComponent>(out var constructComp))
            {
                var result = await TryConstructEntity(constructComp, handEnt, msg.User);

                // TryConstructEntity may delete the existing entity

                msg.Handled = result;
            }
            else // try to start the deconstruction process
            {
                // A tool was not used on the entity.
                if (!handEnt.TryGetComponent<IToolComponent>(out var toolComp))
                    return;

                // no known recipe for entity
                if (!_craftRecipes.TryGetValue(targetPrototype.ID, out var prototype))
                    return;

                // there is a recipe, but it can't be deconstructed.
                var lastStep = prototype.Stages[^1].Backward;
                if (!(lastStep is ConstructionStepTool))
                    return;

                // wrong tool
                var caps = ((ConstructionStepTool) lastStep).ToolQuality;
                if ((toolComp.Qualities & caps) == 0)
                    return;

                // ask around and see if the deconstruction prerequisites are satisfied
                // (remove bulbs, approved access, open panels, etc)
                var deconCompMsg = new BeginDeconstructCompMsg(msg.User);
                targetEnt.SendMessage(null, deconCompMsg);
                if(deconCompMsg.BlockDeconstruct)
                    return;

                var deconEntMsg = new BeginDeconstructEntityMsg(msg.User, handEnt, targetEnt);
                RaiseLocalEvent(deconEntMsg);
                if(deconEntMsg.BlockDeconstruct)
                    return;

                // --- GOOD TO GO ---
                msg.Handled = true;

                // pop off the material and switch to frame
                var targetEntPos = targetEnt.Transform.MapPosition;
                if (prototype.Stages.Count <= 2) // there are no intermediate stages
                {
                    targetEnt.Delete();

                    SpawnIngredient(targetEntPos, prototype.Stages[(prototype.Stages.Count - 2)].Forward as ConstructionStepMaterial);
                }
                else // replace ent with intermediate
                {
                    // Spawn frame
                    var frame = SpawnCopyTransform("structureconstructionframe", targetEnt.Transform);
                    var construction = frame.GetComponent<ConstructionComponent>();
                    SetupComponent(construction, prototype);
                    construction.Stage = prototype.Stages.Count - 2;
                    SetupDeconIntermediateSprite(construction, prototype);
                    frame.Transform.LocalRotation = targetEnt.Transform.LocalRotation;

                    if (targetEnt.Prototype.Components.TryGetValue("Item", out var itemProtoComp))
                    {
                        if(frame.HasComponent<ItemComponent>())
                            frame.RemoveComponent<ItemComponent>();

                        var itemComp = frame.AddComponent<ItemComponent>();

                        var serializer = YamlObjectSerializer.NewReader(itemProtoComp);
                        itemComp.ExposeData(serializer);
                    }

                    ReplaceInContainerOrGround(targetEnt, frame);

                    // remove target
                    targetEnt.Delete();

                    // spawn material
                    SpawnIngredient(targetEntPos, prototype.Stages[(prototype.Stages.Count-2)].Forward as ConstructionStepMaterial);
                }
            }
        }

        private IEntity SpawnCopyTransform(string prototypeId, ITransformComponent toReplace)
        {
            var frame = EntityManager.SpawnEntity(prototypeId, toReplace.MapPosition);
            frame.Transform.WorldRotation = toReplace.WorldRotation;
            frame.Transform.ParentUid = toReplace.ParentUid;
            return frame;
        }

        private static void SetupDeconIntermediateSprite(ConstructionComponent constructionComponent, ConstructionPrototype prototype)
        {
            if(!constructionComponent.Owner.TryGetComponent<SpriteComponent>(out var spriteComp))
                return;

            for (var i = prototype.Stages.Count - 1; i >= 0; i--)
            {
                if (prototype.Stages[i].Icon != null)
                {
                    spriteComp.AddLayerWithSprite(prototype.Stages[1].Icon);
                    return;
                }
            }

            spriteComp.AddLayerWithSprite(prototype.Icon);
        }

        public void SpawnIngredient(MapCoordinates position, ConstructionStepMaterial lastStep)
        {
            if(lastStep is null)
                return;

            var material = lastStep.Material;
            var quantity = lastStep.Amount;

            var matEnt = EntityManager.SpawnEntity(MaterialPrototypes[material], position);
            if (matEnt.TryGetComponent<StackComponent>(out var stackComp))
            {
                stackComp.Count = quantity;
            }
            else
            {
                quantity--; // already spawned one above
                while (quantity > 0)
                {
                    EntityManager.SpawnEntity(MaterialPrototypes[material], position);
                    quantity--;
                }
            }
        }

        private static readonly Dictionary<ConstructionStepMaterial.MaterialType, string> MaterialPrototypes =
            new Dictionary<ConstructionStepMaterial.MaterialType, string>
            {
                { ConstructionStepMaterial.MaterialType.Cable, "CableStack1" },
                { ConstructionStepMaterial.MaterialType.Gold, "GoldStack1" },
                { ConstructionStepMaterial.MaterialType.Metal, "SteelSheet1" },
                { ConstructionStepMaterial.MaterialType.Glass, "GlassSheet1" }
            };

        private bool TryStartStructureConstruction(IEntity placingEnt, GridCoordinates loc, string prototypeName, Angle angle)
        {
            var prototype = _prototypeManager.Index<ConstructionPrototype>(prototypeName);

            if (!InteractionChecks.InRangeUnobstructed(placingEnt, loc.ToMap(_mapManager),
                ignoredEnt: placingEnt, ignoreInsideBlocker: prototype.CanBuildInImpassable))
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
            if(!placingEnt.TryGetComponent<HandsComponent>(out var hands))
            {
                return false;
            };
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
            Get<AudioSystem>().PlayAtCoords("/Audio/Items/deconstruct.ogg", loc);
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
            Get<AudioSystem>().PlayFromEntity("/Audio/Items/deconstruct.ogg", placingEnt);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = SpawnCopyTransform(prototype.Result, placingEnt.Transform);
                hands.PutInHandOrDrop(ent.GetComponent<ItemComponent>());
            }
            else
            {
                var frame = SpawnCopyTransform("structureconstructionframe", placingEnt.Transform);
                var construction = frame.GetComponent<ConstructionComponent>();
                SetupComponent(construction, prototype);

                var finalPrototype = _prototypeManager.Index<EntityPrototype>(prototype.Result);
                if (finalPrototype.Components.TryGetValue("Item", out var itemProtoComp))
                {
                    if(frame.HasComponent<ItemComponent>())
                        frame.RemoveComponent<ItemComponent>();

                    var itemComp = frame.AddComponent<ItemComponent>();

                    var serializer = YamlObjectSerializer.NewReader(itemProtoComp);
                    itemComp.ExposeData(serializer);

                    hands.PutInHandOrDrop(itemComp);
                }
            }
        }

        private async Task<bool> TryConstructEntity(ConstructionComponent constructionComponent, IEntity handTool, IEntity user)
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

            if (await TryProcessStep(constructEntity, stage.Forward, handTool, user, transformComponent.GridPosition))
            {
                constructionComponent.Stage++;
                if (constructionComponent.Stage == constructPrototype.Stages.Count - 1)
                {
                    // Oh boy we get to finish construction!
                    var ent = SpawnCopyTransform(constructPrototype.Result, transformComponent);
                    ent.Transform.LocalRotation = transformComponent.LocalRotation;

                    ReplaceInContainerOrGround(constructEntity, ent);

                    constructEntity.Delete();
                    return true;
                }

                stage = constructPrototype.Stages[constructionComponent.Stage];
                if (stage.Icon != null)
                {
                    spriteComponent.LayerSetSprite(0, stage.Icon);
                }
            }

            else if (await TryProcessStep(constructEntity, stage.Backward, handTool, user, transformComponent.GridPosition))
            {
                constructionComponent.Stage--;
                stage = constructPrototype.Stages[constructionComponent.Stage];

                // If forward needed a material, drop it
                SpawnIngredient(constructEntity.Transform.MapPosition, stage.Forward as ConstructionStepMaterial);

                if (constructionComponent.Stage == 0)
                {
                    // Deconstruction complete.
                    constructEntity.Delete();
                    return true;
                }

                if (stage.Icon != null)
                {
                    spriteComponent.LayerSetSprite(0, stage.Icon);
                }
            }

            return true;
        }

        private static void ReplaceInContainerOrGround(IEntity oldEntity, IEntity newEntity)
        {
            var parentEntity = oldEntity.Transform.Parent?.Owner;
            if (!(parentEntity is null) && parentEntity.TryGetComponent<IContainerManager>(out var containerMan))
            {
                if (containerMan.TryGetContainer(oldEntity, out var container))
                {
                    container.ForceRemove(oldEntity);
                    container.Insert(newEntity);
                }
            }
        }

        private async Task<bool> TryProcessStep(IEntity constructEntity, ConstructionStep step, IEntity slapped, IEntity user, GridCoordinates gridCoords)
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
                        sound.PlayAtCoords("/Audio/Items/zip.ogg", gridCoords);
                    else
                        sound.PlayAtCoords("/Audio/Items/deconstruct.ogg", gridCoords);
                    return true;
                case ConstructionStepTool toolStep:
                    if (!slapped.TryGetComponent<ToolComponent>(out var tool))
                        return false;

                    // Handle welder manually since tool steps specify fuel amount needed, for some reason.
                    if (toolStep.ToolQuality.HasFlag(ToolQuality.Welding))
                        return slapped.TryGetComponent<WelderComponent>(out var welder)
                               && await welder.UseTool(user, constructEntity, toolStep.DoAfterDelay, toolStep.ToolQuality, toolStep.Amount);

                    return await tool.UseTool(user, constructEntity, toolStep.DoAfterDelay, toolStep.ToolQuality);

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

        private void SetupComponent(ConstructionComponent constructionComponent, ConstructionPrototype prototype)
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

            var frame = constructionComponent.Owner;
            var finalPrototype = _prototypeManager.Index<EntityPrototype>(prototype.Result);

            frame.Name = $"Unfinished {finalPrototype.Name}";
        }
    }

    /// <summary>
    /// A system message that is raised when an entity is trying to be deconstructed.
    /// </summary>
    public class BeginDeconstructEntityMsg : EntitySystemMessage
    {
        /// <summary>
        /// Entity that initiated the deconstruction.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        /// Tool in the active hand of the user.
        /// </summary>
        public IEntity Hand { get; }

        /// <summary>
        /// Target entity that is trying to be deconstructed.
        /// </summary>
        public IEntity Target { get; }

        /// <summary>
        /// Set this to true if you would like to block the deconstruction from happening.
        /// </summary>
        public bool BlockDeconstruct { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="BeginDeconstructEntityMsg"/>.
        /// </summary>
        /// <param name="user">Entity that initiated the deconstruction.</param>
        /// <param name="hand">Tool in the active hand of the user.</param>
        /// <param name="target">Target entity that is trying to be deconstructed.</param>
        public BeginDeconstructEntityMsg(IEntity user, IEntity hand, IEntity target)
        {
            User = user;
            Hand = hand;
            Target = target;
        }
    }

    /// <summary>
    /// A component message that is raised when an entity is trying to be deconstructed.
    /// </summary>
    public class BeginDeconstructCompMsg : ComponentMessage
    {
        /// <summary>
        /// Entity that initiated the deconstruction.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        /// Set this to true if you would like to block the deconstruction from happening.
        /// </summary>
        public bool BlockDeconstruct { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="BeginDeconstructCompMsg"/>.
        /// </summary>
        /// <param name="user">Entity that initiated the deconstruction.</param>
        public BeginDeconstructCompMsg(IEntity user)
        {
            User = user;
        }
    }
}
