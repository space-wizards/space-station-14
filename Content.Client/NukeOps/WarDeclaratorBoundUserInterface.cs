using Content.Shared.NukeOps;
using Robust.Client.GameObjects;
using System.Threading;
using Content.Client.Stylesheets;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Timing;
using Content.Client.GameTicking.Managers;

namespace Content.Client.NukeOps
{
    /// <summary>
    /// War declarator that used in NukeOps game rule for declaring war
    /// </summary>
    public sealed class WarDeclaratorBoundUserInterface : BoundUserInterface
    {
        
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private WarDeclaratorWindow? _window;
        public WarDeclaratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) {}
        private TimeSpan _endTime;
        private TimeSpan _timeStamp;
        private WarConditionStatus _status;

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
            _window.OnDraw += UpdateTimer;
        }

        private void OnMessageChanged(string newMsg)
        {
            SendMessage(new WarDeclaratorChangedMessage(newMsg));
        }

        private void OnWarButtonPressed()
        {
            SendMessage(new WarDeclaratorPressedWarButton(_window?.MessageLineEdit.Text));
        }

        public void UpdateTimer()
        {
            if (_window == null)
            {
                return;
            }

            switch(_status)
            {
                case WarConditionStatus.YES_WAR:
                    var gameruleTime = _gameTiming.CurTime.Subtract(_timeStamp);
                    var timeLeft = _endTime.Subtract(gameruleTime);

                    if (timeLeft > TimeSpan.Zero)
                    {
                        _window.InfoLabel.Text = Loc.GetString("war-declarator-boost-timer", ("minutes", timeLeft.Minutes), ("seconds", timeLeft.Seconds));
                    }
                    else
                    {
                        _status = WarConditionStatus.NO_WAR_TIMEOUT;
                        _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-impossible");
                        _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-time-out");
                        _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateNone);
                        _window.WarButton.Disabled = true;
                    }
                    break;
                case WarConditionStatus.WAR_DELAY:
                    var timeAfterDeclaration = _gameTiming.CurTime.Subtract(_timeStamp);
                    var timeRemain = _endTime.Subtract(timeAfterDeclaration);

                    if (timeRemain > TimeSpan.Zero)
                    {
                        _window.InfoLabel.Text = Loc.GetString("war-declarator-boost-timer", ("minutes", timeRemain.Minutes), ("seconds", timeRemain.Seconds));
                    }
                    else
                    {
                        _status = WarConditionStatus.WAR_READY;
                        _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-declared");
                        _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-ready");
                        _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                        _window.WarButton.Disabled = true;
                    }
                    break;
                default:
                    return;
            }
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
            _timeStamp = cast.TimeStamp;
            _endTime = cast.EndTime;
            _status = cast.Status;

            switch(cast.Status)
            {
                case WarConditionStatus.WAR_READY:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-declared");
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-ready");
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                    break;
                case WarConditionStatus.WAR_DELAY:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-declared-delay");
                    UpdateTimer();
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateLow);
                    break;
                case WarConditionStatus.YES_WAR:
                    _window.StatusLabel.Text = Loc.GetString("war-declarator-boost-possible");
                    UpdateTimer();
                    _window.StatusLabel.SetOnlyStyleClass(StyleNano.StyleClassPowerStateGood);
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
                    _window.InfoLabel.Text = Loc.GetString("war-declarator-conditions-unknown");
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
