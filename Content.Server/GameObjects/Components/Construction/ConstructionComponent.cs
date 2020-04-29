using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using static Content.Shared.Construction.ConstructionStepMaterial;
using static Content.Shared.Construction.ConstructionStepTool;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionComponent : Component, IAttackBy
    {
        public override string Name => "Construction";

        [ViewVariables]
        public ConstructionPrototype Prototype { get; private set; }
        [ViewVariables]
        public int Stage { get; private set; }

        SpriteComponent Sprite;
        ITransformComponent Transform;
#pragma warning disable 649
        [Dependency] private IRobustRandom _random;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            Sprite = Owner.GetComponent<SpriteComponent>();
            Transform = Owner.GetComponent<ITransformComponent>();
            var systemman = IoCManager.Resolve<IEntitySystemManager>();
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var playerEntity = eventArgs.User;
            var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
            if (!interactionSystem.InRangeUnobstructed(playerEntity.Transform.MapPosition, Owner.Transform.WorldPosition, ignoredEnt: Owner, insideBlockerValid: Prototype.CanBuildInImpassable))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, playerEntity,
                    _localizationManager.GetString("You can't reach there!"));
                return false;
            }

            var stage = Prototype.Stages[Stage];

            if (TryProcessStep(stage.Forward, eventArgs.AttackWith))
            {
                Stage++;
                if (Stage == Prototype.Stages.Count - 1)
                {
                    // Oh boy we get to finish construction!
                    var entMgr = IoCManager.Resolve<IServerEntityManager>();
                    var ent = entMgr.SpawnEntity(Prototype.Result, Transform.GridPosition);
                    ent.GetComponent<ITransformComponent>().LocalRotation = Transform.LocalRotation;
                    Owner.Delete();
                    return true;
                }

                stage = Prototype.Stages[Stage];
                if (stage.Icon != null)
                {
                    Sprite.LayerSetSprite(0, stage.Icon);
                }
            }

            else if (TryProcessStep(stage.Backward, eventArgs.AttackWith))
            {
                Stage--;
                if (Stage == 0)
                {
                    // Deconstruction complete.
                    Owner.Delete();
                    return true;
                }

                stage = Prototype.Stages[Stage];
                if (stage.Icon != null)
                {
                    Sprite.LayerSetSprite(0, stage.Icon);
                }
            }

            return true;
        }

        public void Init(ConstructionPrototype prototype)
        {
            Prototype = prototype;
            Stage = 1;
            if(prototype.Stages[1].Icon != null)
            {
                Sprite.AddLayerWithSprite(prototype.Stages[1].Icon);
            }
            else
            {
                Sprite.AddLayerWithSprite(prototype.Icon);
            }


        }

        bool TryProcessStep(ConstructionStep step, IEntity slapped)
        {
            if (step == null)
            {
                return false;
            }
            var sound = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();

            switch (step)
            {
                case ConstructionStepMaterial matStep:
                    if (!slapped.TryGetComponent(out StackComponent stack)
                     || !MaterialStackValidFor(matStep, stack)
                     || !stack.Use(matStep.Amount))
                    {
                        return false;
                    }
                    if (matStep.Material == MaterialType.Cable)
                        sound.Play("/Audio/items/zip.ogg", Transform.GridPosition);
                    else
                        sound.Play("/Audio/items/deconstruct.ogg", Transform.GridPosition);
                    return true;
                case ConstructionStepTool toolStep:
                    if (!slapped.TryGetComponent<ToolComponent>(out var tool))
                        return false;
                    switch (toolStep.Tool)
                    {
                        case Tool.Crowbar:
                            if (tool.Behavior == Tool.Crowbar)
                            {
                                tool.PlayUseSound();
                                return true;
                            }
                            return false;
                        case Tool.Welder:
                            if (tool.Behavior == Tool.Welder && tool.TryWeld(toolStep.Amount))
                            {
                                tool.PlayUseSound();
                                return true;
                            }
                            return false;
                        case Tool.Wrench:
                            if (tool.Behavior == Tool.Wrench)
                            {
                                tool.PlayUseSound();
                                return true;
                            }
                            return false;
                        case Tool.Screwdriver:
                            if (tool.Behavior == Tool.Screwdriver)
                            {
                                tool.PlayUseSound();
                                return true;
                            }
                            return false;
                        case Tool.Wirecutter:
                            if (tool.Behavior == Tool.Wirecutter)
                            {
                                tool.PlayUseSound();
                                return true;
                            }
                            return false;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static Dictionary<StackType, MaterialType> StackTypeMap
        = new Dictionary<StackType, MaterialType>
        {
            { StackType.Cable, MaterialType.Cable },
            { StackType.Glass, MaterialType.Glass },
            { StackType.Metal, MaterialType.Metal }
        };

        // Really this should check the actual materials at play..
        public static bool MaterialStackValidFor(ConstructionStepMaterial step, StackComponent stack)
        {
            return StackTypeMap.TryGetValue((StackType)stack.StackType, out var should) && should == step.Material;
        }
    }
}
