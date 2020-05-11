using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Close the last EntityStorage we opened
    /// This will also update the State for it (which a regular InteractWith won't do)
    /// </summary>
    public sealed class CloseLastStorageOperator : IOperator
    {
        private readonly IEntity _owner;

        public CloseLastStorageOperator(IEntity owner)
        {
            _owner = owner;
        }
        
        public Outcome Execute(float frameTime)
        {
            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);

            if (blackboard == null)
            {
                return Outcome.Failed;
            }

            var target = blackboard.GetState<LastOpenedStorageState>().GetValue();
        
            if ((target.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length >
                InteractionSystem.InteractionRange)
            {
                return Outcome.Failed;
            }

            if (!target.TryGetComponent(out EntityStorageComponent storageComponent) || 
                storageComponent.Locked)
            {
                return Outcome.Failed;
            }
            
            if (storageComponent.Open)
            {
                storageComponent.ToggleOpen();
            }

            blackboard.GetState<LastOpenedStorageState>().SetValue(null);

            return Outcome.Success;
        }
    }
}