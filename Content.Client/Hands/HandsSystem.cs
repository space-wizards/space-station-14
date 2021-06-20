using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.Hands
{
    internal sealed class HandsSystem : SharedHandsSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>((_, component, _) => component.SettupGui());
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>((_, component, _) => component.ClearGui());

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
            if (session == null)
                return;

            var player = session.AttachedEntity;

            if (player == null)
                return;

            if (!player.TryGetComponent(out SharedHandsComponent? hands))
                return;

            if (!hands.TryGetSwapHandsResult(out var nextHand))
                return;

            EntityManager.RaisePredictiveEvent(new RequestSetHandEvent(nextHand));
        }

        private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session == null)
                return false;

            var player = session.AttachedEntity;

            if (player == null)
                return false;

            if (!player.TryGetComponent(out SharedHandsComponent? hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            EntityManager.RaisePredictiveEvent(new RequestDropHeldEntityEvent(activeHand, coords));
            return true;
        }

        protected override void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args)
        {
            component.HandsModified();
        }
    }
}
