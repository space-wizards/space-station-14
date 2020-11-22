using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Nutrition;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Inventory;
using Content.Server.AI.Utility.Considerations.Nutrition.Drink;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Nutrition.Drink
{
    public sealed class UseDrinkInInventory : UtilityAction
    {
        private readonly IEntity _entity;

        public UseDrinkInInventory(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, _entity),
                new UseDrinkInInventoryOperator(Owner, _entity),
            });
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
                considerationsManager.Get<TargetInOurInventoryCon>()
                    .BoolCurve(context),
                considerationsManager.Get<DrinkValueCon>()
                    .QuadraticCurve(context, 1.0f, 0.4f, 0.0f, 0.0f),
            };
        }
    }
}
