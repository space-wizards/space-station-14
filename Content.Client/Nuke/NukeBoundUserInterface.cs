using Content.Client.Traitor.Uplink;
using Content.Shared.Nuke;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Nuke
{
    [UsedImplicitly]
    public sealed class NukeBoundUserInterface : BoundUserInterface
    {
        private NukeMenu? _menu;

        public NukeBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new NukeMenu();
            _menu.OpenCentered();
            _menu.OnClose += Close;

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
                SendMessage(new NukeEjectMessage());
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
                {
                    _menu.UpdateState(msg);
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Close();
            _menu?.Dispose();
        }
    }
}
