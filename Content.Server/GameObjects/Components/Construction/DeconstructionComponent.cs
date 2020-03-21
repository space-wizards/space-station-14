using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Construction;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using static Content.Shared.Construction.ConstructionStepMaterial;
using static Content.Shared.Construction.ConstructionStepTool;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent] 
    public class DeconstructionComponent : Component, IAttackBy
    {
        public override string Name => "Deconstruction";

        [ViewVariables]
        public ConstructionPrototype GotoPrototype { get; private set; }
        [ViewVariables]
        public int GotoStage { get; private set; }
        [ViewVariables]
        public ToolType DeconTool { get; private set; }
        [ViewVariables]
        public int ToolAmount { get; private set; } // Currently just for welders

        SpriteComponent Sprite;
        ITransformComponent Transform;
#pragma warning disable 649
        [Dependency] private IRobustRandom _random;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private readonly IPrototypeManager    _prototypeManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            Sprite = Owner.GetComponent<SpriteComponent>();
            Transform = Owner.GetComponent<ITransformComponent>();
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var playerEntity = eventArgs.User;
            var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
            if (!interactionSystem.InRangeUnobstructed(playerEntity.Transform.MapPosition, Owner.Transform.WorldPosition, ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, playerEntity,
                    _localizationManager.GetString("You can't reach there!"));
                return false;
            }

            if (CheckTool(DeconTool, eventArgs.AttackWith))
            {
                // Spawn in-progress prototype
                var entMgr = IoCManager.Resolve<IServerEntityManager>();
                var frame = _serverEntityManager.SpawnEntity("structureconstructionframe", Transform.GridPosition);
                frame.GetComponent<ITransformComponent>().LocalRotation = Transform.LocalRotation;

                var construction = frame.GetComponent<ConstructionComponent>();
                construction.Init(GotoPrototype);
                construction.SetStage(GotoStage);
                Owner.Delete();

            }

            return true;
        }

        bool CheckTool(ToolType tool, IEntity slapped)
        {
            var sound = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();

            switch(tool)
            {
                case ToolType.Crowbar:
                    if (slapped.HasComponent<CrowbarComponent>())
                    {
                        sound.Play("/Audio/items/crowbar.ogg", Transform.GridPosition);
                        return true;
                    }
                    return false;

                case ToolType.Welder:
                    if (slapped.TryGetComponent(out WelderComponent welder) && welder.TryUse(ToolAmount))
                    {
                        if (_random.NextDouble() > 0.5)
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
                        if (_random.NextDouble() > 0.5)
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
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            String _gotoPrototypeName = "";
            String _gotoPrototypeStage = "";
            String _deconTool = "";
            String _toolAmount = "";

            serializer.DataField(ref _gotoPrototypeName, "gotoprototype", "ERROR");
            serializer.DataField(ref _gotoPrototypeStage, "gotostage", "1");
            serializer.DataField(ref _deconTool, "decontool", "Wrench", true);
            serializer.DataField(ref _toolAmount, "toolamount", "0");

            GotoPrototype = _prototypeManager.Index<ConstructionPrototype>(_gotoPrototypeName);
            GotoStage = Convert.ToInt32(_gotoPrototypeStage);
            DeconTool = Enum.Parse<ToolType>(_deconTool);
            ToolAmount = Convert.ToInt32(_toolAmount);
        }


        public enum ToolType
        {
            Wrench,
            Welder,
            Screwdriver,
            Crowbar,
            Wirecutters,
        }
    }
}
