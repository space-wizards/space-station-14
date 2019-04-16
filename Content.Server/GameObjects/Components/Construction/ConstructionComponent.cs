using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Content.Shared.Construction.ConstructionStepMaterial;
using static Content.Shared.Construction.ConstructionStepTool;

namespace Content.Server.GameObjects.Components.Construction
{
    public class ConstructionComponent : Component, IAttackBy
    {
        public override string Name => "Construction";

        [ViewVariables]
        public ConstructionPrototype Prototype { get; private set; }
        [ViewVariables]
        public int Stage { get; private set; }

        SpriteComponent Sprite;
        ITransformComponent Transform;
        Random random;

        public override void Initialize()
        {
            base.Initialize();

            Sprite = Owner.GetComponent<SpriteComponent>();
            Transform = Owner.GetComponent<ITransformComponent>();
            var systemman = IoCManager.Resolve<IEntitySystemManager>();
            random = new Random();
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var stage = Prototype.Stages[Stage];

            if (TryProcessStep(stage.Forward, eventArgs.AttackWith))
            {
                Stage++;
                if (Stage == Prototype.Stages.Count - 1)
                {
                    // Oh boy we get to finish construction!
                    var entMgr = IoCManager.Resolve<IServerEntityManager>();
                    var ent = entMgr.ForceSpawnEntityAt(Prototype.Result, Transform.GridPosition);
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
            Sprite.AddLayerWithSprite(prototype.Stages[1].Icon);
        }

        bool TryProcessStep(ConstructionStep step, IEntity slapped)
        {
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
                    switch (toolStep.Tool)
                    {
                        case ToolType.Crowbar:
                            if (slapped.HasComponent<CrowbarComponent>())
                            {
                                sound.Play("/Audio/items/crowbar.ogg", Transform.GridPosition);
                                return true;
                            }
                            return false;
                        case ToolType.Welder:
                            if (slapped.TryGetComponent(out WelderComponent welder) && welder.TryUse(toolStep.Amount))
                            {
                                if (random.NextDouble() > 0.5)
                                    sound.Play("/Audio/items/welder.ogg", Transform.GridPosition);
                                else
                                    sound.Play("/Audio/items/welder2.ogg", Transform.GridPosition);
                                return true;
                            }
                            return false;
                        case ToolType.Wrench:
                            if (slapped.HasComponent<WrenchComponent>())
                            {
                                sound.Play("/Audio/items/ratchet.ogg", Transform.GridPosition);
                                return true;
                            }
                            return false;
                        case ToolType.Screwdriver:
                            if (slapped.HasComponent<ScrewdriverComponent>())
                            {
                                if (random.NextDouble() > 0.5)
                                    sound.Play("/Audio/items/screwdriver.ogg", Transform.GridPosition);
                                else
                                    sound.Play("/Audio/items/screwdriver2.ogg", Transform.GridPosition);
                                return true;
                            }
                            return false;
                        case ToolType.Wirecutters:
                            if (slapped.HasComponent<WirecutterComponent>())
                            {
                                sound.Play("/Audio/items/wirecutter.ogg", Transform.GridPosition);
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

        private static Dictionary<StackType, ConstructionStepMaterial.MaterialType> StackTypeMap
        = new Dictionary<StackType, ConstructionStepMaterial.MaterialType>
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
