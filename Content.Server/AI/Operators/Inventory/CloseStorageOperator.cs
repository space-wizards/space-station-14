using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Close the last EntityStorage we opened
    /// This will also update the State for it (which a regular InteractWith won't do)
    /// </summary>
    public sealed class CloseLastStorageOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private EntityUid _target;

        public CloseLastStorageOperator(EntityUid owner)
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

            return _target != default;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);

            blackboard?.GetState<LastOpenedStorageState>().SetValue(default);
            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_target == default || !EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(_owner, _target, popup: true))
            {
                return Outcome.Failed;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(_target, out EntityStorageComponent? storageComponent) ||
                storageComponent.IsWeldedShut)
            {
                return Outcome.Failed;
            }
            
            if (entMan.EntitySysManager.TryGetEntitySystem<EntityStorageSystem>(out var entStorage) && storageComponent.Open)
            {
                entStorage.ToggleOpen(_owner, _target, storageComponent);
            }

            return Outcome.Success;
        }
    }
}
