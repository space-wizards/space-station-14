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
            if (step == null)
            {
                return false;
            }
            var sound = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
            sound.Play(step.AudioClip);
            return true;
            //switch (step)
            //{
            //    case constructionstepmaterial matstep:
            //        if (!slapped.trygetcomponent(out stackcomponent stack)
            //         || !materialstackvalidfor(matstep, stack)
            //         || !stack.use(matstep.amount))
            //        {
            //            return false;
            //        }
            //        if (matstep.material == materialtype.cable)
            //            sound.play("/audio/items/zip.ogg", transform.gridposition);
            //        else
            //            sound.play("/audio/items/deconstruct.ogg", transform.gridposition);
            //        return true;
            //    case constructionsteptool toolstep:
            //        switch (toolstep.tool)
            //        {
            //            case tooltype.crowbar:
            //                if (slapped.hascomponent<crowbarcomponent>())
            //                {
            //                    sound.play("/audio/items/crowbar.ogg", transform.gridposition);
            //                    return true;
            //                }
            //                return false;
            //            case tooltype.welder:
            //                if (slapped.trygetcomponent(out weldercomponent welder) && welder.tryuse(toolstep.amount))
            //                {
            //                    if (_random.nextdouble() > 0.5)
            //                        sound.play("/audio/items/welder.ogg", transform.gridposition);
            //                    else
            //                        sound.play("/audio/items/welder2.ogg", transform.gridposition);
            //                    return true;
            //                }
            //                return false;
            //            case tooltype.wrench:
            //                if (slapped.hascomponent<wrenchcomponent>())
            //                {
            //                    sound.play("/audio/items/ratchet.ogg", transform.gridposition);
            //                    return true;
            //                }
            //                return false;
            //            case tooltype.screwdriver:
            //                if (slapped.hascomponent<screwdrivercomponent>())
            //                {
            //                    if (_random.nextdouble() > 0.5)
            //                        sound.play("/audio/items/screwdriver.ogg", transform.gridposition);
            //                    else
            //                        sound.play("/audio/items/screwdriver2.ogg", transform.gridposition);
            //                    return true;
            //                }
            //                return false;
            //            case tooltype.wirecutters:
            //                if (slapped.hascomponent<wirecuttercomponent>())
            //                {
            //                    sound.play("/audio/items/wirecutter.ogg", transform.gridposition);
            //                    return true;
            //                }
            //                return false;
            //            default:
            //                throw new notimplementedexception();
            //        }
            //    default:
            //        throw new notimplementedexception();
            }
        }
}
