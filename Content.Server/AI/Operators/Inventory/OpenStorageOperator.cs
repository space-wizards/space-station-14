using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// If the target is in EntityStorage will open its parent container
    /// </summary>
    public sealed class OpenStorageOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _target;

        public OpenStorageOperator(EntityUid owner, EntityUid target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!_target.TryGetContainer(out var container))
            {
                return Outcome.Success;
            }

            if (!_owner.InRangeUnobstructed(container, popup: true))
            {
                return Outcome.Failed;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(container.Owner, out EntityStorageComponent? storageComponent) ||
                storageComponent.IsWeldedShut)
            {
                return Outcome.Failed;
            }

            if (!storageComponent.Open)
            {
                var activateArgs = new ActivateEventArgs(_owner, _target);
                storageComponent.Activate(activateArgs);
            }

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);
            blackboard?.GetState<LastOpenedStorageState>().SetValue(container.Owner);

            return Outcome.Success;
        }
    }
}
