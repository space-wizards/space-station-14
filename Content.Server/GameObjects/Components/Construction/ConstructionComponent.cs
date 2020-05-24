using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using static Content.Shared.Construction.ConstructionStepMaterial;
using static Content.Shared.Construction.ConstructionStepTool;
using Robust.Shared.Utility;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionComponent : Component, IInteractUsing
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
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // default interaction check for AttackBy allows inside blockers, so we will check if its blocked if
            // we're not allowed to build on impassable stuff
            if (Prototype.CanBuildInImpassable == false)
            {
                if (!InteractionChecks.InRangeUnobstructed(eventArgs, false))
                    return false;
            }

            var stage = Prototype.Stages[Stage];

            if (TryProcessStep(stage.Forward, eventArgs.Using, eventArgs.User))
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

            else if (TryProcessStep(stage.Backward, eventArgs.Using, eventArgs.User))
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

        bool TryProcessStep(ConstructionStep step, IEntity slapped, IEntity user)
        {
            if (step == null)
            {
                return false;
            }
            var sound = EntitySystem.Get<AudioSystem>();

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

                    // Handle welder manually since tool steps specify fuel amount needed, for some reason.
                    if (toolStep.ToolQuality.HasFlag(ToolQuality.Welding))
                        return slapped.TryGetComponent<WelderComponent>(out var welder)
                               && welder.UseTool(user, Owner, toolStep.ToolQuality, toolStep.Amount);

                    return tool.UseTool(user, Owner, toolStep.ToolQuality);

                default:
                    throw new NotImplementedException();
            }
        }

        private static Dictionary<StackType, MaterialType> StackTypeMap
        = new Dictionary<StackType, MaterialType>
        {
            { StackType.Cable, MaterialType.Cable },
            { StackType.Gold, MaterialType.Gold },
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
