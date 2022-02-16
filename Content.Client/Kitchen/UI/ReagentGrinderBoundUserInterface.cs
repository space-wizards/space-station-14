using Content.Shared.Kitchen.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Content.Shared.Chemistry.Components.Solution;

namespace Content.Client.Kitchen.UI
{
    public sealed class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private GrinderMenu? _menu;

        public ReagentGrinderBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();

            _menu = new GrinderMenu(this, _entityManager, _prototypeManager);
            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _menu?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is ReagentGrinderInterfaceState cState))
            {
                return;
            }

            _menu?.UpdateState(cState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _menu?.HandleMessage(message);
        }

        public void StartGrinding(BaseButton.ButtonEventArgs? args = null) => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderGrindStartMessage());
        public void StartJuicing(BaseButton.ButtonEventArgs? args = null) => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderJuiceStartMessage());
        public void EjectAll(BaseButton.ButtonEventArgs? args = null) => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectChamberAllMessage());
        public void EjectBeaker(BaseButton.ButtonEventArgs? args = null) => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectBeakerMessage());
        public void EjectChamberContent(EntityUid uid) => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectChamberContentMessage(uid));
    }
}
