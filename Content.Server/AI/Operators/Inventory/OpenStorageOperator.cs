#nullable enable
using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Utility;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// If the target is in EntityStorage will open its parent container
    /// </summary>
    public sealed class OpenStorageOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _target;

        public OpenStorageOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!ContainerHelpers.TryGetContainer(_target, out var container))
            {
                return Outcome.Success;
            }

            if (!_owner.InRangeUnobstructed(container, popup: true))
            {
                return Outcome.Failed;
            }

            if (!container.Owner.TryGetComponent(out EntityStorageComponent? storageComponent) ||
                storageComponent.IsWeldedShut)
            {
                return Outcome.Failed;
            }

            if (!storageComponent.Open)
            {
                var activateArgs = new ActivateEventArgs {User = _owner, Target = _target};
                storageComponent.Activate(activateArgs);
            }

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);
            blackboard?.GetState<LastOpenedStorageState>().SetValue(container.Owner);

            return Outcome.Success;
        }
    }
}
