using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.TapeRecorder.Ui;

[UsedImplicitly]
public sealed class TapeRecorderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private TapeRecorderMenu? _menu;

    [ViewVariables]
    private TimeSpan _printCooldown;

    protected override void Open()
    {
        base.Open();

        _menu = new(this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void ToggleSwitch()
    {
        SendMessage(new ToggleTapeRecorderMessage());
    }

    public void ChangeMode(TapeRecorderMode mode)
    {
        SendMessage(new ChangeModeTapeRecorderMessage(mode));
    }

    public void PrintTranscript()
    {
        SendMessage(new PrintTapeRecorderMessage());

        if (_menu != null)
            _menu.UpdatePrint(true);

        Timer.Spawn(_printCooldown, () =>
        {
            if (_menu != null)
                _menu.UpdatePrint(false);
        });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (TapeRecorderState) state;

        _printCooldown = castState.PrintCooldown;

        _menu?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}

