using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

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

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(_owner, container.Owner, popup: true))
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
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<EntityStorageSystem>().ToggleOpen(_owner, _target, storageComponent);
            }

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);
            blackboard?.GetState<LastOpenedStorageState>().SetValue(container.Owner);

            return Outcome.Success;
        }
    }
}
