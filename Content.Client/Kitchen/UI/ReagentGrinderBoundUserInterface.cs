using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Kitchen.UI
{
    public sealed class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        private GrinderMenu? _menu;

        public ReagentGrinderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new GrinderMenu(this, EntMan, _prototypeManager);
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
            if (state is not ReagentGrinderInterfaceState cState)
                return;

            _menu?.UpdateState(cState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _menu?.HandleMessage(message);
        }

        public void StartGrinding(BaseButton.ButtonEventArgs? _ = null)
        {
            SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Grind));
        }

        public void StartJuicing(BaseButton.ButtonEventArgs? _ = null)
        {
            SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Juice));
        }

        public void EjectAll(BaseButton.ButtonEventArgs? _ = null)
        {
            SendMessage(new ReagentGrinderEjectChamberAllMessage());
        }

        public void EjectBeaker(BaseButton.ButtonEventArgs? _ = null)
        {
            SendMessage(new ItemSlotButtonPressedEvent(SharedReagentGrinder.BeakerSlotId));
        }

        public void EjectChamberContent(EntityUid uid)
        {
            SendMessage(new ReagentGrinderEjectChamberContentMessage(EntMan.GetNetEntity(uid)));
        }
    }
}
