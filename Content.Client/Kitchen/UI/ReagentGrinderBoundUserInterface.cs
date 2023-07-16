using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Kitchen.UI
{
    public sealed class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private GrinderMenu? _menu;

        public ReagentGrinderBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

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

        public void StartGrinding(BaseButton.ButtonEventArgs? args = null) => SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Grind));
        public void StartJuicing(BaseButton.ButtonEventArgs? args = null) => SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Juice));
        public void EjectAll(BaseButton.ButtonEventArgs? args = null) => SendMessage(new ReagentGrinderEjectChamberAllMessage());
        public void EjectBeaker(BaseButton.ButtonEventArgs? args = null) => SendMessage(new ItemSlotButtonPressedEvent(SharedReagentGrinder.BeakerSlotId));
        public void EjectChamberContent(EntityUid uid) => SendMessage(new ReagentGrinderEjectChamberContentMessage(uid));
    }
}
