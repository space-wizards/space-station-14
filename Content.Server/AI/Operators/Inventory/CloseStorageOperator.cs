using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Close the last EntityStorage we opened
    /// This will also update the State for it (which a regular InteractWith won't do)
    /// </summary>
    public sealed class CloseLastStorageOperator : AiOperator
    {
        private readonly IEntity _owner;
        private IEntity? _target;

        public CloseLastStorageOperator(IEntity owner)
        {
            _owner = owner;
        }

        public override bool Startup()
        {
            if (!base.Startup())
            {
                return true;
            }

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);

            if (blackboard == null)
            {
                return false;
            }

            _target = blackboard.GetState<LastOpenedStorageState>().GetValue();

            return _target != null;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);

            blackboard?.GetState<LastOpenedStorageState>().SetValue(null);
            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_target == null || !_owner.InRangeUnobstructed(_target, popup: true))
            {
                return Outcome.Failed;
            }

            if (!_target.TryGetComponent(out EntityStorageComponent? storageComponent) ||
                storageComponent.IsWeldedShut)
            {
                return Outcome.Failed;
            }

            if (storageComponent.Open)
            {
                var activateArgs = new ActivateEventArgs(_owner, _target);
                storageComponent.Activate(activateArgs);
            }

            return Outcome.Success;
        }
    }
}
