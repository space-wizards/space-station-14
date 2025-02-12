using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Nuke
{
    [UsedImplicitly]
    public sealed class NukeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private NukeMenu? _menu;

        public NukeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = this.CreateWindow<NukeMenu>();

            _menu.OnKeypadButtonPressed += i =>
            {
                SendMessage(new NukeKeypadMessage(i));
            };
            _menu.OnEnterButtonPressed += () =>
            {
                SendMessage(new NukeKeypadEnterMessage());
            };
            _menu.OnClearButtonPressed += () =>
            {
                SendMessage(new NukeKeypadClearMessage());
            };

            _menu.EjectButton.OnPressed += _ =>
            {
                SendMessage(new ItemSlotButtonPressedEvent(SharedNukeComponent.NukeDiskSlotId));
            };
            _menu.AnchorButton.OnPressed += _ =>
            {
                SendMessage(new NukeAnchorMessage());
            };
            _menu.ArmButton.OnPressed += _ =>
            {
                SendMessage(new NukeArmedMessage());
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
                return;

            switch (state)
            {
                case NukeUiState msg:
                    _menu.UpdateState(msg);
                    break;
            }
        }
    }
}
