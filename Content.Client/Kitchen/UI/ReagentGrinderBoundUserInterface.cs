using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Kitchen.UI
{
    public sealed class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GrinderMenu? _menu;

        public ReagentGrinderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<GrinderMenu>();
            _menu.OnToggleAuto += ToggleAutoMode;
            _menu.OnGrind += StartGrinding;
            _menu.OnJuice += StartJuicing;
            _menu.OnEjectAll += EjectAll;
            _menu.OnEjectBeaker += EjectBeaker;
            _menu.OnEjectChamber += EjectChamberContent;
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

        public void ToggleAutoMode()
        {
            SendMessage(new ReagentGrinderToggleAutoModeMessage());
        }

        public void StartGrinding()
        {
            SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Grind));
        }

        public void StartJuicing()
        {
            SendMessage(new ReagentGrinderStartMessage(GrinderProgram.Juice));
        }

        public void EjectAll()
        {
            SendMessage(new ReagentGrinderEjectChamberAllMessage());
        }

        public void EjectBeaker()
        {
            SendMessage(new ItemSlotButtonPressedEvent(SharedReagentGrinder.BeakerSlotId));
        }

        public void EjectChamberContent(EntityUid uid)
        {
            SendMessage(new ReagentGrinderEjectChamberContentMessage(EntMan.GetNetEntity(uid)));
        }
    }
}
