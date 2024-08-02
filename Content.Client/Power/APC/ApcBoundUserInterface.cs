using Content.Client.Power.APC.UI;
using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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
            _menu.OnBreaker += BreakerPressed;
        }

        protected override void UpdateState(IBoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _menu?.UpdateState(state);
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }
    }
}
