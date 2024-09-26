using Content.Client.Power.APC.UI;
using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Power.APC
{
    [UsedImplicitly]
    public sealed class ApcBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ApcMenu? _menu;

        public ApcBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ApcMenu>();
            _menu.SetEntity(Owner);
            _menu.OnBreaker += BreakerPressed;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;
            _menu?.UpdateState(castState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            if (message is ApcAccessCheckMessage)
            {
                _menu?.SetAccessEnabled(((ApcAccessCheckMessage)message).HasAccess);
            }
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }
    }
}
