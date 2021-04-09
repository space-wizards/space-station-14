using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
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
                .Register<HandsSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<HandsSystem>();
            base.Shutdown();
        }

        private void SwapHandsPressed(ICommonSession? session)
        {
            EntityManager.RaisePredictiveEvent(new SwapHandsMessage());
        }

        protected override void HandleContainerModified(ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out SharedHandsComponent? hands))
                hands.HandsModified();
        }
    }
}
