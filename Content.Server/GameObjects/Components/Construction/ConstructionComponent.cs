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
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;


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
            var stage = Prototype.Stages[Stage];

            if (TryProcessStep(stage.Forward, eventArgs.AttackWith))
            {
                Stage++;
                if (Stage == Prototype.Stages.Count - 1)
                {
                    // Oh boy we get to finish construction!
                    var entMgr = IoCManager.Resolve<IServerEntityManager>();
                    var ent = entMgr.SpawnEntityAt(Prototype.Result, Transform.GridPosition);
                    ent.GetComponent<ITransformComponent>().LocalRotation = Transform.LocalRotation;
                    Owner.Delete();
                    return true;
                }

                stage = Prototype.Stages[Stage];
                if (stage.Icon != null)
                {
                    Sprite.LayerSetSprite(0, stage.Icon);
                }
                else
                {
                    Sprite.LayerSetSprite(0, Prototype.Icon);
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
            if (prototype.Stages[1].Icon != null)
            {
                Sprite.AddLayerWithSprite(prototype.Stages[1].Icon);
            }
            
        }

        bool TryProcessStep(ConstructionStep step, IEntity slapped)
        {
            if (step == null)
            {
                return false;
            }
            var sound = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();           
            if(slapped.TryGetComponent(out StackComponent stack))
            {
                stack.Use(step.Amount);
            }
            sound.Play(step.AudioClip, Transform.GridPosition);
            return true;
           }
        }
}
