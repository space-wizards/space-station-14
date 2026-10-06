using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI;

[UsedImplicitly]
public sealed partial class SignalTimerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

    [ViewVariables]
    private SignalTimerWindow? _window;

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
        SendPredictedMessage(new SignalTimerStartMessage());
    }

    private void OnTextChanged(string newText)
    {
        SendPredictedMessage(new SignalTimerTextChangedMessage(newText));
    }

    private void OnDelayChanged(string newDelay)
    {
        if (_window == null)
            return;

        SendPredictedMessage(new SignalTimerDelayChangedMessage(_window.GetDelay()));
    }

    public override void Update()
    {
        base.Update();

        if (_window == null
            || !EntMan.TryGetComponent(Owner, out SignalTimerComponent? comp))
            return;

        var time = EntMan.TryGetComponent<ActiveSignalTimerComponent>(Owner, out var active)
            ? active.TriggerTime
            : TimeSpan.Zero;

        _window.SetCurrentText(comp.Label);
        _window.SetCurrentDelay(comp.Delay);
        _window.SetShowText(comp.CanEditLabel);
        _window.SetTriggerTime(time);
        _window.SetTimerStarted(active != null);
        _window.SetHasAccess(_playerMan.LocalEntity != null && _accessReader.IsAllowed(_playerMan.LocalEntity.Value, Owner));
    }
}
