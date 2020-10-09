using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Close the last EntityStorage we opened
    /// This will also update the State for it (which a regular InteractWith won't do)
    /// </summary>
    public sealed class CloseLastStorageOperator : AiOperator
    {
        private readonly IEntity _owner;
        private IEntity _target;

        public CloseLastStorageOperator(IEntity owner)
        {
            _owner = owner;
        }

        public override bool TryStartup()
        {
            if (!base.TryStartup())
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

        public override void Shutdown(Outcome outcome)
        {
            base.Shutdown(outcome);
            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);

            blackboard?.GetState<LastOpenedStorageState>().SetValue(null);
        }

        public override Outcome Execute(float frameTime)
        {
            if (!_owner.InRangeUnobstructed(_target, popup: true))
            {
                return Outcome.Failed;
            }

            if (!_target.TryGetComponent(out EntityStorageComponent storageComponent) ||
                storageComponent.IsWeldedShut)
            {
                return Outcome.Failed;
            }

            if (storageComponent.Open)
            {
                var activateArgs = new ActivateEventArgs {User = _owner, Target = _target};
                storageComponent.Activate(activateArgs);
            }

            return Outcome.Success;
        }
    }
}
