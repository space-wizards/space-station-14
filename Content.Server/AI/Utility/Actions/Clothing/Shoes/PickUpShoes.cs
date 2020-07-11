using System;
using System.Collections.Generic;
using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Clothing;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Inventory;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Clothing.Shoes
{
    public sealed class PickUpShoes : UtilityAction
    {
        private readonly IEntity _entity;

        public PickUpShoes(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new GoPickupEntitySequence(Owner, _entity).Sequence;
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<ClothingInSlotCon>().Slot(EquipmentSlotDefines.Slots.SHOES, context)
                    .InverseBoolCurve(context),
                considerationsManager.Get<CanPutTargetInHandsCon>()
                    .BoolCurve(context),
                considerationsManager.Get<ClothingInInventoryCon>().Slot(EquipmentSlotDefines.SlotFlags.SHOES, context)
                    .InverseBoolCurve(context),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}
