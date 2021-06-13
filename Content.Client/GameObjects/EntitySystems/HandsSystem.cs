using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.EntitySystems
{
    internal sealed class HandsSystem : SharedHandsSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed))
                .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
                .Register<HandsSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<HandsSystem>();
            base.Shutdown();
        }

        private void SwapHandsPressed(ICommonSession? session)
        {
            EntityManager.RaisePredictiveEvent(new RequestSwapHandsevent());
        }

        private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            EntityManager.RaisePredictiveEvent(new RequestDropHeldEntityEvent(coords));
            return true;
        }

        protected override void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args)
        {
            component.HandsModified();
        }
    }
}
