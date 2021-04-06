using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);

            //SubscribeNetworkEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);
            //SubscribeLocalEvent<CombatModeSystemMessages.SetCombatModeActiveMessage>(CombatModeActiveHandler);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedHandsSystem>();
            base.Shutdown();
        }

        protected abstract void HandleContainerModified(ContainerModifiedMessage args);
    }
}
