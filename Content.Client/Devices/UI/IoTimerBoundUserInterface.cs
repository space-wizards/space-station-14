using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class IoTimerBoundUserInterface : BoundUserInterface
    {
        private IoTimerWindow? _window;

        public IoTimerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new IoTimerWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnStartTimerPressed += StartTimer;
            _window.OnSetDurationPressed += SetDuration;
            _window.OnPauseTimerPressed += TogglePauseTimer;
            _window.OnResetTimerPressed += ResetTimer;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not IoTimerBoundUserInterfaceState cast)
                return;

            _window.TimerActive = cast.IsActive;
            _window.Duration = cast.Duration;
            _window.StartAndEndTimes = cast.StartAndEndTimes;
            _window.TimerPaused = cast.IsPaused;
        }

        private void SetDuration(int duration)
        {
            SendMessage(new IoTimerUpdateDurationMessage(duration));
        }

        private void ResetTimer()
        {
            SendMessage(new IoTimerSendResetMessage());
        }

        private void TogglePauseTimer()
        {
            SendMessage(new IoTimerSendTogglePauseMessage());
        }

        private void StartTimer()
        {
            SendMessage(new IoTimerSendToggleMessage());
        }

        private void UpdateDuration(int duration)
        {
            SendMessage(new IoTimerUpdateDurationMessage(duration));
        }
    }
}
