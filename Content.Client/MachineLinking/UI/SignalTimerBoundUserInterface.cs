using Content.Shared.MachineLinking;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.MachineLinking.UI;

public sealed partial class SignalTimerBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId EndTimer = new("end");
    private static readonly EntityTimerId RefreshTimer = new("refresh");

    [ViewVariables]
    private SignalTimerWindow? _window;

    public SignalTimerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SignalTimerWindow>();
        _window.OnStartTimer += StartTimer;
        _window.OnCurrentTextChanged += OnTextChanged;
        _window.OnCurrentDelayMinutesChanged += OnDelayChanged;
        _window.OnCurrentDelaySecondsChanged += OnDelayChanged;
    }

    public void StartTimer()
    {
        SendMessage(new SignalTimerStartMessage());
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
        base.UpdateState(state);

        if (_window == null || state is not SignalTimerBoundUserInterfaceState cast)
            return;

        _window.SetCurrentText(cast.CurrentText);
        _window.SetCurrentDelayMinutes(cast.CurrentDelayMinutes);
        _window.SetCurrentDelaySeconds(cast.CurrentDelaySeconds);
        _window.SetShowText(cast.ShowText);
        _window.SetTriggerTime(cast.TriggerTime);
        _window.SetTimerStarted(cast.TimerStarted);
        _window.SetHasAccess(cast.HasAccess);
        if (!cast.TimerStarted)
        {
            CancelTimer(EndTimer);
            CancelTimer(RefreshTimer);
            _window.UpdateTimer(TimeSpan.Zero);
            return;
        }

        SetTimerAt(EndTimer, cast.TriggerTime);
        SetTimer(RefreshTimer, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        UpdateRemainingTime();
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == EndTimer)
            CancelTimer(RefreshTimer);

        if (timer.Id == EndTimer || timer.Id == RefreshTimer)
            UpdateRemainingTime();
    }

    private void UpdateRemainingTime()
    {
        var remaining = TryGetTimer(EndTimer, out var timer) ? timer.Remaining : TimeSpan.Zero;
        _window?.UpdateTimer(remaining);
    }
}
