using Content.Shared.Input;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class HandsSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();
            input.BindMap.BindFunction(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(HandleSwapHands));
            input.BindMap.BindFunction(ContentKeyFunctions.Drop, InputCmdHandler.FromDelegate(HandleDrop));
            input.BindMap.BindFunction(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem));
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (EntitySystemManager.TryGetEntitySystem(out InputSystem input))
            {
                input.BindMap.UnbindFunction(ContentKeyFunctions.SwapHands);
                input.BindMap.UnbindFunction(ContentKeyFunctions.Drop);
                input.BindMap.UnbindFunction(ContentKeyFunctions.ActivateItemInHand);
            }

            base.Shutdown();
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T : Component
        {
            component = default(T);

            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.SwapHands();
        }

        private static void HandleDrop(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.Drop(handsComp.ActiveIndex);
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }
    }
}
