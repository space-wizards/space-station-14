using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.MachineLinking.UI
{
    public sealed class SignalTimerBoundUserInterface : BoundUserInterface
    {
        private SignalTimerWindow? _window;

        public SignalTimerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new SignalTimerWindow(this);
            if (State != null)
                UpdateState(State);

            Logger.DebugS("UIST", "STBUI Opened.");

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnCurrentTextEntered += OnTextChanged;
            _window.OnCurrentDelayMinutesEntered += OnDelayChanged;
            _window.OnCurrentDelaySecondsEntered += OnDelayChanged;

        }
        public void OnStartTimer()
        {
            SendMessage(new SignalTimerStartMessage(Owner.Owner));
        }

        private void OnTextChanged(string newText)
        {
            SendMessage(new SignalTimerTextChangedMessage(newText));
        }

        private void OnDelayChanged(string newDelay)
        {
            if (_window == null)
                return;
            SendMessage(new SignalTimerDelayChangedMessage(_window.GetDelay()));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            Logger.DebugS("UIST", "STBUI UpdateState called.");

            base.UpdateState(state);

            if (_window == null || state is not SignalTimerBoundUserInterfaceState cast)
                return;

            Logger.DebugS("UIST", "STBUI updatestate setting text to "+cast.CurrentText+", and time to "+cast.CurrentDelayMinutes+":"+cast.CurrentDelaySeconds);

            _window.SetCurrentText(cast.CurrentText);
            _window.SetCurrentDelayMinutes(cast.CurrentDelayMinutes);
            _window.SetCurrentDelaySeconds(cast.CurrentDelaySeconds);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
