using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.BodySystem;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class SurgeryToolComponent : Component, IAfterAttack
    {

        #pragma warning disable 649
                [Dependency] private readonly IMapManager _mapManager;
                [Dependency] private readonly IEntitySystemManager _entitySystemManager;
                [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private SurgeryToolType _surgeryToolClass;
        private float _baseOperateTime;

        public override string Name => "SurgeryTool";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _surgeryToolClass, "surgeryToolClass", SurgeryToolType.Incision);
            serializer.DataField(ref _baseOperateTime, "baseOperateTime", 5);
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
                return;
            if (eventArgs.Attacked.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager))
            {
                //List<string> options = new List();
                //options = string version of bodyManager.Parts;
                //PopupListSelection(options, SelectCallback);
            }
            //var curTime = IoCManager.Resolve<IGameTiming>().CurTime;
            //var location = eventArgs.User.Transform.GridPosition;
            //var angle = new Angle(eventArgs.ClickLocation.ToMapPos(_mapManager) - location.ToMapPos(_mapManager));

        }
        private void SelectCallback(BodyManagerComponent bodyManager, BodyPart target)
        {
            bodyManager.AttemptSurgery(target, _surgeryToolClass);
        }

        protected virtual void AfterSelect(BodyManagerComponent bodyManager, BodyPart target)
        {
            bodyManager.AttemptSurgery(target, _surgeryToolClass);
        }
    }
}
