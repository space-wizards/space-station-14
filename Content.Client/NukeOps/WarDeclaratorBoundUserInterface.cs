using Content.Shared.NukeOps;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Content.Client.Stylesheets;

namespace Content.Client.NukeOps
{
    /// <summary>
    /// War declarator that used in NukeOps game rule for declaring war
    /// </summary>
    public sealed class WarDeclaratorBoundUserInterface : BoundUserInterface
    {
        private WarDeclaratorWindow? _window;

        public WarDeclaratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new WarDeclaratorWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnMessageEntered += OnMessageChanged;
            _window.OnWarButtonPressed += OnWarButtonPressed;

        }

        private void OnMessageChanged(string newMsg)
        {
            SendMessage(new WarDeclaratorChangedMessage(newMsg));
        }

        private void OnWarButtonPressed()
        {
            SendMessage(new WarDeclaratorPressedWarButton(_window?.MessageLineEdit.Text));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not WarDeclaratorBoundUserInterfaceState cast)
                return;

            _window.WarButton.Disabled = cast.Status != WarConditionStatus.YES_WAR;

            switch(cast.Status)
            {
                case WarConditionStatus.YES_WAR:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-possible");
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-boost-timer", ("minutes", "timeLeft.Minutes"), ("seconds", "timeLeft.Seconds"));
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
                    break;
                case WarConditionStatus.TC_DISTRIBUTED:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-distributed");
                    _window.InfoLabel.Text = String.Empty;
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                    break;
                case WarConditionStatus.NO_WAR_SMALL_CREW:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-impossible");
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-small-crew", ("min_size", cast.MinCrew));
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
                case WarConditionStatus.NO_WAR_SHUTTLE_DEPARTED:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-impossible");
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-left-outpost");
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
                case WarConditionStatus.NO_WAR_TIMEOUT:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-impossible");
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-time-out");
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
                default:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-impossible");
                    _window.InfoLabel.Text = String.Empty;
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                    break;
            }

            _window.SetMessage(cast.Message);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
